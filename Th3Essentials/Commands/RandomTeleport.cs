using System;
using System.Collections.Generic;
using Th3Essentials.Config;
using Th3Essentials.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal class RandomTeleport : Command
{
    private Th3PlayerConfig _playerConfig = null!;

    private Th3Config _config = null!;

    private ICoreServerAPI _sapi = null!;

    // List for specific locations
    private List<Vec3i>? _pos;

    internal override void Init(ICoreServerAPI api)
    {
        _sapi = api;
        _playerConfig = Th3Essentials.PlayerConfig;
        _config = Th3Essentials.Config;

        
        if (Th3Essentials.Config.RandomTeleportRadius > 0)
        {
            _pos = _sapi.LoadModConfig<List<Vec3i>>("th3rtplocations.json");
            _sapi = api;       
            api.ChatCommands.Create("rtp")
                .WithDescription(Lang.Get("th3essentials:cd-rtp"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(OnRtp)
                
                .BeginSubCommand("item")
                    .RequiresPrivilege(Privilege.controlserver)
                    .RequiresPlayer()
                    .WithDescription(Lang.Get("th3essentials:cd-rtp-desc"))
                    .HandleWith(SetItem)
                .EndSubCommand()
                ;
        }
    }
    
    private TextCommandResult SetItem(TextCommandCallingArgs args)
    {
        var slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;
        
        if (slot.Itemstack == null)
        {
            _config.TeleportToPlayerItem = null;
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-item-unset"));
        }
        var enumItemClass = slot.Itemstack.Class;
        var stackSize = slot.Itemstack.StackSize;
        var code = slot.Itemstack.Collectible.Code;

        if (slot.Itemstack.Attributes is not TreeAttribute attributes) return TextCommandResult.Success("error not a TreeAttribute");
                        
        // remove food perish data
        attributes.RemoveAttribute("transitionstate");

        _config.RandomTeleportItem = new StarterkitItem(enumItemClass, code, stackSize, attributes);
        _config.MarkDirty();
        return TextCommandResult.Success(Lang.Get("th3essentials:hs-item-set"));
    }


    private TextCommandResult OnRtp(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player;
        var playerData = _playerConfig.GetPlayerDataByUid(player.PlayerUID);

        var playerConfig = Homesystem.GetConfig(player, playerData, _config);

        if (!playerConfig.RtpEnabled)
        {
            return TextCommandResult.Success(Lang.Get("th3essentials:cd-all-notallow"));
        }
        
        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
        {
            var spawn = player.Entity.Pos.AsBlockPos;
            var x = Random.Shared.Next(-Th3Essentials.Config.RandomTeleportRadius, Th3Essentials.Config.RandomTeleportRadius);
            var z = Random.Shared.Next(-Th3Essentials.Config.RandomTeleportRadius / 2, Th3Essentials.Config.RandomTeleportRadius / 2);
            BlockPos pos;
            if (_pos?.Count > 0)
            {
                var next = Random.Shared.Next(_pos.Count);
                pos = _pos[next].ToBlockPos();
            }
            else
            {
                pos = new BlockPos(spawn.X + x, 1, spawn.Z + z, 0);
                pos.X = Math.Clamp(pos.X, 0, _sapi.WorldManager.MapSizeX - 1);
                pos.Z = Math.Clamp(pos.Z, 0, _sapi.WorldManager.MapSizeZ - 1);
            }

            if (Homesystem.CheckPayment(_config.RandomTeleportItem, playerConfig.RandomTeleportCost, player, out var canTeleport, out var success)) return success!;
            
            if (canTeleport)
            {
                Homesystem.PayIfNeeded(player, _config.RandomTeleportItem, playerConfig.RandomTeleportCost);
                
                _sapi.WorldManager.LoadChunkColumnPriority(pos.X / _sapi.WorldManager.ChunkSize,
                    pos.Z / GlobalConstants.ChunkSize, new ChunkLoadOptions{ OnLoaded = () =>
                    {
                        // only use terrain height for none list positions
                        if (_pos == null)
                        {
                            var y = _sapi.World.BlockAccessor.GetRainMapHeightAt(pos);
                            pos.Y = y + 1;
                        }

                        TeleportTo(player, playerData, pos);
                    }});
                return TextCommandResult.Success(Lang.Get("th3essentials:rtp-success"));
            }

            return TextCommandResult.Error("Something went wrong");
        }
        
        var diff = playerData.RTPLastUsage.AddSeconds(_config.RandomTeleportCooldown) - DateTime.Now;
        return TextCommandResult.Success(Lang.Get("th3essentials:wait-time", Th3Util.PrettyTime(diff)));
    }

    public static void TeleportTo(IPlayer player, Th3PlayerData playerData, BlockPos location)
    {
        player.Entity.TeleportTo(new Vec3d(location.X + 0.5,location.Y + 0.2,location.Z + 0.5));
        playerData.RTPLastUsage = DateTime.Now;
        playerData.MarkDirty();
    }

    public static bool CanTravel(Th3PlayerData playerData)
    {
        var canTravel = playerData.RTPLastUsage.AddSeconds(Th3Essentials.Config.RandomTeleportCooldown);
        return canTravel <= DateTime.Now;
    }
}