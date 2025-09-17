using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using WoopEssentials.Systems.Data;

namespace WoopEssentials.Systems;

internal class AntiGriefsystem : IDisposable
{
    public static AntiGriefsystem Instance { get; private set; } = null!;

    private ICoreServerAPI _sapi = null!;
    private BlockChangeDatabase? _db;

    internal void Init(ICoreServerAPI sapi)
    {
        Instance = this;
        _sapi = sapi;

        // Block Events
        _sapi.Event.DidBreakBlock += OnDidBreakBlock; // detect each block broken (after break)
        _sapi.Event.DidPlaceBlock += OnDidPlaceBlock; // detect each time a block is placed

        // Entity Events
        _sapi.Event.OnEntityDeath += OnEntityDeath; // detect each time an entity dies

        _sapi.Event.SaveGameLoaded += OnSaveGameLoading; // open DB
        _sapi.Event.GameWorldSave += OnSaveGameSaving; // no-op for now
    }

    private void OnSaveGameSaving()
    {
        // Intentionally left blank. Using WAL, no explicit flush needed.
    }

    private void OnSaveGameLoading()
    {
        try
        {
            _db?.Dispose();
            _db = BlockChangeDatabase.OpenOrCreate(_sapi);
            _sapi.Logger.Event("[WoopEssentials] Block change DB initialized.");
        }
        catch (Exception)
        {
            // Error already logged in DB class. Keep running without DB.
            _db = null;
        }
    }

    private void OnEntityDeath(Entity entity, DamageSource damageSource)
    {
        // Not part of block change tracking. Reserved for future.
    }

    private void OnDidPlaceBlock(IServerPlayer byPlayer, int blockId, BlockSelection blockSel, ItemStack withItemStack)
    {
        if (_db == null) return;
        try
        {
            var pos = blockSel.Position.Copy();

            // After placement, fetch new block id from world
            var newblockId = _sapi.World.BlockAccessor.GetBlock(pos).Id;
            var uid = byPlayer.PlayerUID;
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _db.Log(ts, uid, action: 1, pos.X, pos.Y, pos.Z, newblockId);
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error("[WoopEssentials] onPlaceBlock logging failed: {0}", ex);
        }
    }

    private void OnDidBreakBlock(IServerPlayer byPlayer, int blockId, BlockSelection blockSel)
    {
        if (_db == null) return;
        try
        {
            var pos = blockSel.Position.Copy();

            var uid = byPlayer.PlayerUID;
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _db.Log(ts, uid, action: 0, pos.X, pos.Y, pos.Z, blockId);
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error("[WoopEssentials] onDidBreakBlock logging failed: {0}", ex);
        }
    }

    internal BlockChangeDatabase.BlockChangeEvent[] GetHistoryAt(BlockPos pos, int limit)
    {
        if (_db == null) return [];

        var list = _db.QueryAt(pos.X, pos.Y, pos.Z, limit);
        return list.ToArray();
    }

    public void Dispose()
    {
        _db?.Dispose();
        _db = null;
    }
}