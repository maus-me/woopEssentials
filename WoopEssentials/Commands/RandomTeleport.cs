using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using WoopEssentials.Config;
using WoopEssentials.Systems;

namespace WoopEssentials.Commands;

internal class RandomTeleport : Command
{
    private WoopPlayerConfig _playerConfig = null!;

    private WoopConfig _config = null!;

    private ICoreServerAPI _sapi = null!;

    // List for specific locations
    private List<Vec3i>? _pos;

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

        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
        {
            var spawn = player.Entity.Pos.AsBlockPos;
            var x = Random.Shared.Next(-WoopEssentials.Config.RandomTeleportRadius, WoopEssentials.Config.RandomTeleportRadius);
            var z = Random.Shared.Next(-WoopEssentials.Config.RandomTeleportRadius / 2, WoopEssentials.Config.RandomTeleportRadius / 2);
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
                return TextCommandResult.Success(Lang.Get("woopessentials:rtp-success"));
            }

            return TextCommandResult.Error("Something went wrong");
        }

        var diff = playerData.RTPLastUsage.AddSeconds(_config.RandomTeleportCooldown) - DateTime.Now;
        return TextCommandResult.Success(Lang.Get("woopessentials:wait-time", WoopUtil.PrettyTime(diff)));
    }

    public static void TeleportTo(IPlayer player, WoopPlayerData playerData, BlockPos location)
    {
        player.Entity.TeleportTo(new Vec3d(location.X + 0.5,location.Y + 0.2,location.Z + 0.5));
        playerData.RTPLastUsage = DateTime.Now;
        playerData.MarkDirty();
    }

    public static bool CanTravel(WoopPlayerData playerData)
    {
        var canTravel = playerData.RTPLastUsage.AddSeconds(WoopEssentials.Config.RandomTeleportCooldown);
        return canTravel <= DateTime.Now;
    }
}