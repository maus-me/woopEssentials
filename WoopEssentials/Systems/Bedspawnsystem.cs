using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using WoopEssentials.Config;

namespace WoopEssentials.Systems;

/// <summary>
/// Simple bed spawn system: when a player uses a bed, set their spawn to that bed.
/// When the bed is destroyed, clear the spawn.
/// </summary>
internal class BedSpawnSystem
{
    private ICoreServerAPI _sapi = null!;
    private WoopPlayerConfig _playerConfig = null!;

    internal void Init(ICoreServerAPI sapi)
    {
        _sapi = sapi;
        _playerConfig = WoopEssentials.PlayerConfig;

        // When a player uses a block (e.g., right-clicks a bed), set spawn
        _sapi.Event.DidUseBlock += OnDidUseBlock;
        // When a bed is destroyed, clear spawn for the owner
        _sapi.Event.DidBreakBlock += OnDidBreakBlock;
        // On death, inform the player or fall back if bed missing
        _sapi.Event.PlayerDeath += OnPlayerDeath;
    }

    private void OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
    {
        var data = _playerConfig.GetPlayerDataByUid(byPlayer.PlayerUID, false);
        if (data?.BedPos == null)
        {
            return;
        }

        // If the bed is gone, clear spawn
        var block = _sapi.World.BlockAccessor.GetBlock(data.BedPos);
        if (!IsBedBlock(block))
        {
            // Bed destroyed while dead
            ClearBedSpawn(byPlayer, data, notify: true, destroyedWhileDead: true);
            return;
        }

        // Bed still exists: just inform the player
        byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-spawn-respawn"), EnumChatType.Notification);
    }

    private void OnDidBreakBlock(IServerPlayer byPlayer, int blockId, BlockSelection blockSel)
    {
        var brokenPos = blockSel.Position;
        var brokenBlock = _sapi.World.BlockAccessor.GetBlock(brokenPos);
        if (!IsBedBlock(brokenBlock)) return;

        var normalizedPosition = GetNormalizedBedPosition(brokenBlock, brokenPos);

        foreach (var (uid, pdata) in _playerConfig.Players)
        {
            if (pdata.BedPos == null || !pdata.BedPos.Equals(normalizedPosition)) continue;
            if (_sapi.World.PlayerByUid(uid) is IServerPlayer player)
            {
                ClearBedSpawn(player, pdata, notify: true);
                return;
            }

            // Offline player: clear without notify, will have no spawn set next login
            pdata.BedPos = null;
            pdata.MarkDirty();

        }
    }

    private BlockPos GetNormalizedBedPosition(Block bed, BlockPos pos)
    {
        if (bed.Variant["part"] == "head")
        {
            return pos;
        }

        var currentSide = bed.Variant["side"];

        var headFacing = BlockFacing.FromCode(currentSide)
            .Opposite;

        return pos.AddCopy(headFacing);
    }

    private void OnDidUseBlock(IServerPlayer byPlayer, BlockSelection blockSel)
    {
        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);

        if (!IsBedBlock(block)) return;

        var normalizedPosition = GetNormalizedBedPosition(block, blockSel.Position);

        var currentSpawnPos = byPlayer.GetSpawnPosition(false).AsBlockPos;

        if (currentSpawnPos == normalizedPosition)
        {
            // They used the same bed, so don't do anything
            return;
        }

        // Prevent two players from sharing the same bed spawn
        foreach (var entry in _playerConfig.Players)
        {
            var uid = entry.Key;
            var pdata = entry.Value;
            if (uid == byPlayer.PlayerUID) continue; // allow re-claiming own bed

            // Ordered from what should be the cheapest check to the most expensive.

            // Check if bed is already claimed
            if (pdata.BedPos != null &&
                pdata.BedPos.Equals(normalizedPosition))
            {
                byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-spawn-claimed"), EnumChatType.Notification);
                return;
            }

            // Check if bed is not inside a room
            if (!BlockInRoom(byPlayer.Entity.World.Api, normalizedPosition.UpCopy()))
            {
                byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-not-in-room"), EnumChatType.Notification);
                return;
            }

            // Check if bed is below sea level
            if (normalizedPosition.Y < byPlayer.Entity.World.SeaLevel)
            {
                byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-below-sealevel"), EnumChatType.Notification);
                return;
            }
        }

        var data = _playerConfig.GetPlayerDataByUid(byPlayer.PlayerUID);
        data.BedPos = blockSel.Position.Copy();
        data.MarkDirty();

        byPlayer.SetSpawnPosition(
            new(
                normalizedPosition.X,
                normalizedPosition.Y,
                normalizedPosition.Z
                )
            );

        byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-spawn-set"), EnumChatType.Notification);
    }

    private void ClearBedSpawn(IServerPlayer player, WoopPlayerData data, bool notify, bool destroyedWhileDead = false)
    {
        data.BedPos = null;
        data.MarkDirty();
        player.ClearSpawnPosition();
        if (notify)
        {
            var key = destroyedWhileDead ? "woopessentials:bed-spawn-destroyed" : "woopessentials:bed-spawn-reset";
            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get(key), EnumChatType.Notification);
        }
    }

    private static bool IsBedBlock(Block block)
    {
        var code = block.Code?.Path ?? string.Empty;
        return code.Contains("bed");
    }

    private bool BlockInRoom(ICoreAPI api, BlockPos pos)
    {
        var roomRegistry = api.ModLoader.GetModSystem<RoomRegistry>();

        if (roomRegistry is null)
        {
            return true;
        }

        var skyExposed = api.World.BlockAccessor.GetRainMapHeightAt(pos.X, pos.Z) <= pos.Y;

        if (skyExposed) return false;

        var room = roomRegistry.GetRoomForPosition(pos);

        if (room is null) return false;
        if (room.ExitCount != 0) return false;

        return true;
    }

    /* TODO:
     * Build out functionality to detect when Temporal Gear is used.
     * BUGS:
     * When respawning, the check for the bed existing is occurring after the respawn already happened.  So if the players bed is destroyed, they will still spawn there once.
     * Need to handle this by either moving the check earlier in the respawn process.
     */
}
