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
        // After respawn, optionally break certain beds
        _sapi.Event.PlayerRespawn += OnPlayerRespawn;
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

    private static BlockPos GetNormalizedBedPosition(Block bed, BlockPos pos)
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
            // They used the same bed, so don't do anything.
            return;
        }


        foreach (var (uid, pdata) in _playerConfig.Players)
        {
            if (uid == byPlayer.PlayerUID) continue; // allow re-claiming own bed

            // Ordered from what should be the cheapest check to the most expensive.

            // Prevent two players from sharing the same bed spawn
            if (pdata.BedPos == null ||
                !pdata.BedPos.Equals(normalizedPosition)) continue;

            byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-spawn-claimed"), EnumChatType.Notification);
            return;
        }

        // Prevent spawn points outside of rooms
        if (!BlockInRoom(byPlayer.Entity.World.Api, normalizedPosition.UpCopy()))
        {
            byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-not-in-room"), EnumChatType.Notification);
            return;
        }

        // byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Bed placed at: {normalizedPosition.Y} and Sea level is {byPlayer.Entity.World.SeaLevel}", EnumChatType.Notification);
        // Prevent spawn points underground
        if (normalizedPosition.Y < byPlayer.Entity.World.SeaLevel)
        {
            byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-below-sealevel"), EnumChatType.Notification);
            return;
        }

        var data = _playerConfig.GetPlayerDataByUid(byPlayer.PlayerUID);
        data.BedPos = blockSel.Position.Copy();
        data.MarkDirty();

        byPlayer.SetSpawnPosition(
            new PlayerSpawnPos(
                normalizedPosition.X,
                normalizedPosition.Y,
                normalizedPosition.Z
                )
            );

        byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-spawn-set"), EnumChatType.Notification);
    }

    private static void ClearBedSpawn(IServerPlayer player, WoopPlayerData data, bool notify, bool destroyedWhileDead = false)
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

    private static bool IsBreakableBed(Block block)
    {
        // Only certain beds (e.g., hay bed) are subject to breaking after respawn
        var code = block.Code?.Path ?? string.Empty;
        return code.Contains("hay");
    }

    private void OnPlayerRespawn(IServerPlayer byPlayer)
    {
        // After the player respawns, optionally break certain beds with a chance
        var data = _playerConfig.GetPlayerDataByUid(byPlayer.PlayerUID, false);
        if (data?.BedPos == null) return;

        var ba = _sapi.World.BlockAccessor;
        var bedBlock = ba.GetBlock(data.BedPos);
        if (!IsBedBlock(bedBlock)) return;
        if (!IsBreakableBed(bedBlock)) return;

        // Chance for the bed to break after respawn
        const double breakChance = 0.25; // 25% chance
        if (_sapi.World.Rand.NextDouble() < breakChance)
        {
            BreakBedAt(byPlayer, data.BedPos);
            ClearBedSpawn(byPlayer, data, notify: true);
        } else {
            byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-break-chance"), EnumChatType.Notification);
        }
    }

    private void BreakBedAt(IServerPlayer byPlayer, BlockPos anyHalfPos)
    {
        var ba = _sapi.World.BlockAccessor;
        var block = ba.GetBlock(anyHalfPos);
        if (!IsBedBlock(block)) return;

        // Determine head position for consistency
        var headPos = GetNormalizedBedPosition(block, anyHalfPos);
        var headBlock = ba.GetBlock(headPos);

        // Break head half
        if (IsBedBlock(headBlock))
        {
            ba.BreakBlock(headPos, byPlayer);
        }

        // Determine and break the other half if present
        var sideCode = headBlock?.Variant?["side"]; // direction the bed faces
        if (sideCode != null)
        {
            var facing = BlockFacing.FromCode(sideCode);
            var otherHalfPos = headPos.AddCopy(facing);
            var otherHalfBlock = ba.GetBlock(otherHalfPos);
            if (IsBedBlock(otherHalfBlock))
            {
                ba.BreakBlock(otherHalfPos, byPlayer);
            }
        }

        // Inform the player that their bed was broken
        byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("woopessentials:bed-break"), EnumChatType.Notification);
    }

    private static bool BlockInRoom(ICoreAPI api, BlockPos pos)
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
     */
}
