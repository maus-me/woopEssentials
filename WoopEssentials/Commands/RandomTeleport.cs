using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using WoopEssentials.Config;
using WoopEssentials.Systems;
using WoopEssentials.Systems.EntityBehavior;

namespace WoopEssentials.Commands;

internal class RandomTeleport : Command
{
    private WoopPlayerConfig _playerConfig = null!;

    private WoopConfig _config = null!;

    private ICoreServerAPI _sapi = null!;

    // List for specific locations
    private List<Vec3i>? _pos;

    // Maximum number of attempts to find a safe location
    private const int MaxTeleportAttempts = 5;

    internal override void Init(ICoreServerAPI api)
    {
        if (WoopEssentials.Config.RandomTeleportRadius <= 0) return;

        _sapi = api;
        _playerConfig = WoopEssentials.PlayerConfig;
        _config = WoopEssentials.Config;

        _pos = _sapi.LoadModConfig<List<Vec3i>>("wooprtplocations.json");
        _sapi = api;
        api.ChatCommands.Create("rtp")
            .WithDescription(Lang.Get("woopessentials:cd-rtp"))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnRtp)
            .WithAlias("rt")

            .BeginSubCommand("item")
                .RequiresPrivilege(Privilege.controlserver)
                .RequiresPlayer()
                .WithDescription(Lang.Get("woopessentials:cd-rtp-desc"))
                .HandleWith(SetItem)
            .EndSubCommand()
            ;
    }

    private TextCommandResult SetItem(TextCommandCallingArgs args)
    {
        var slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;

        if (slot.Itemstack == null)
        {
            _config.TeleportToPlayerItem = null;
            return TextCommandResult.Success(Lang.Get("woopessentials:hs-item-unset"));
        }
        var enumItemClass = slot.Itemstack.Class;
        var stackSize = slot.Itemstack.StackSize;
        var code = slot.Itemstack.Collectible.Code;

        if (slot.Itemstack.Attributes is not TreeAttribute attributes) return TextCommandResult.Success("error not a TreeAttribute");

        // remove food perish data
        attributes.RemoveAttribute("transitionstate");

        _config.RandomTeleportItem = new StarterkitItem(enumItemClass, code, stackSize, attributes);
        _config.MarkDirty();
        return TextCommandResult.Success(Lang.Get("woopessentials:hs-item-set"));
    }
    
    private TextCommandResult OnRtp(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player;
        var playerData = _playerConfig.GetPlayerDataByUid(player.PlayerUID);

        var playerConfig = Homesystem.GetConfig(player, playerData, _config);

        if (!playerConfig.RtpEnabled)
        {
            return TextCommandResult.Success(Lang.Get("woopessentials:cd-all-notallow"));
        }

        // Prevent RTP when pvp tagged.
        if (!EntityBehaviorPvp.CheckPvP(player, out var errorMessage))
        {
            return TextCommandResult.Success(errorMessage);
        }

        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
        {
            if (Homesystem.CheckPayment(_config.RandomTeleportItem, playerConfig.RandomTeleportCost, player, out var canTeleport, out var success)) 
                return success!;

            if (canTeleport)
            {
                Homesystem.PayIfNeeded(player, _config.RandomTeleportItem, playerConfig.RandomTeleportCost);
                
                // Start the teleport process
                if (player is IServerPlayer serverPlayer)
                {
                    BlockPos pos = GenerateRandomPosition(player.Entity.Pos.AsBlockPos);
                    AttemptTeleport(serverPlayer, playerData, pos, 1);
                }
                
                return TextCommandResult.Success(Lang.Get("woopessentials:rtp-success"));
            }

            return TextCommandResult.Error("Something went wrong");
        }

        var diff = playerData.RTPLastUsage.AddSeconds(_config.RandomTeleportCooldown) - DateTime.Now;
        return TextCommandResult.Success(Lang.Get("woopessentials:wait-time", WoopUtil.PrettyTime(diff)));
    }
    
    // Generate a random position within the configured radius
    private BlockPos GenerateRandomPosition(BlockPos spawn)
    {
        if (_pos?.Count > 0)
        {
            var next = Random.Shared.Next(_pos.Count);
            return _pos[next].ToBlockPos();
        }
        else
        {
            var x = Random.Shared.Next(-WoopEssentials.Config.RandomTeleportRadius, WoopEssentials.Config.RandomTeleportRadius);
            var z = Random.Shared.Next(-WoopEssentials.Config.RandomTeleportRadius / 2, WoopEssentials.Config.RandomTeleportRadius / 2);
            
            var pos = new BlockPos(spawn.X + x, 1, spawn.Z + z, 0);
            pos.X = Math.Clamp(pos.X, 0, _sapi.WorldManager.MapSizeX - 1);
            pos.Z = Math.Clamp(pos.Z, 0, _sapi.WorldManager.MapSizeZ - 1);
            
            return pos;
        }
    }
    
    // Recursive method to attempt teleporting the player to a safe location
    private void AttemptTeleport(IServerPlayer player, WoopPlayerData playerData, BlockPos pos, int attempt)
    {
        // Load the chunk and check if the location is safe
        _sapi.WorldManager.LoadChunkColumnPriority(
            pos.X / _sapi.WorldManager.ChunkSize,
            pos.Z / GlobalConstants.ChunkSize, 
            new ChunkLoadOptions{ OnLoaded = () =>
            {
                // Calculate Y position based on terrain
                if (_pos == null)
                {
                    var y = _sapi.World.BlockAccessor.GetRainMapHeightAt(pos);
                    pos.Y = y + 1;
                }
                // Even for predefined locations, make sure Y is at least 1
                else if (pos.Y <= 0)
                {
                    pos.Y = 1;
                }
                
                // Check if the location is safe
                if (IsSafeLocation(pos))
                {
                    // Safe location found, teleport the player
                    TeleportTo(player, playerData, pos);
                }
                else
                {
                    if (attempt < MaxTeleportAttempts)
                    {
                        // Try again with a new position
                        BlockPos newPos = GenerateRandomPosition(player.Entity.Pos.AsBlockPos);
                        AttemptTeleport(player, playerData, newPos, attempt + 1);
                    }
                    else
                    {
                        // Max attempts reached, fall back to spawn
                        player.SendMessage(GlobalConstants.GeneralChatGroup, 
                            Lang.Get("woopessentials:rtp-max-attempts-reached"), 
                            EnumChatType.Notification);
                    }
                }
            }});
    }

    private static void TeleportTo(IPlayer player, WoopPlayerData playerData, BlockPos location)
    {
        player.Entity.TeleportTo(new Vec3d(location.X + 0.5,location.Y + 0.2,location.Z + 0.5));
        playerData.RTPLastUsage = DateTime.Now;
        playerData.MarkDirty();
    }

    /* Checks if the target location is safe for teleportation.
     Liquid includes water, lava, and blood.
     TODO: Explore if this check would be better or more efficient with a Material check with "GetBlockMaterial"
    */
    private bool IsSafeLocation(BlockPos pos)
    {
        // Check the target block and a few blocks around it
        var blockAtPos = _sapi.World.BlockAccessor.GetBlock(pos);
        var blockAbove = _sapi.World.BlockAccessor.GetBlock(pos.X, pos.Y + 1, pos.Z);
        var blockBelow = _sapi.World.BlockAccessor.GetBlock(pos.X, pos.Y - 1, pos.Z);

        // Check if any of these positions have liquid
        if (blockAtPos.LiquidLevel > 0 || blockBelow.LiquidLevel > 0 || blockAbove.LiquidLevel > 0)
        {
            return false; // liquid present
        }

        return true; // Location is safe
    }

    private static bool CanTravel(WoopPlayerData playerData)
    {
        var canTravel = playerData.RTPLastUsage.AddSeconds(WoopEssentials.Config.RandomTeleportCooldown);
        return canTravel <= DateTime.Now;
    }
}