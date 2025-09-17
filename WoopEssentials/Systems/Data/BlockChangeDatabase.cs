using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace WoopEssentials.Systems.Data;

/// <summary>
/// Lightweight, append-optimized SQLite store for block change events.
/// File location: {dataPath}/blockchanges.sqlite
/// </summary>
internal sealed class BlockChangeDatabase : IDisposable
{
    internal readonly struct BlockChangeEvent
    {
        public readonly long TsMs;
        public readonly string? PlayerUid;
        public readonly int Action;
        public readonly int? BlockId;

        public BlockChangeEvent(long tsMs, string? playerUid, int action, int? blockId)
        {
            TsMs = tsMs;
            PlayerUid = playerUid;
            Action = action;
            BlockId = blockId;
        }
    }

    private readonly ICoreServerAPI _sapi;
    private SqliteConnection? _conn;
    private SqliteCommand? _insertCmd;

    private BlockChangeDatabase(ICoreServerAPI sapi)
    {
        _sapi = sapi;
    }

    public static BlockChangeDatabase OpenOrCreate(ICoreServerAPI sapi)
    {
        var db = new BlockChangeDatabase(sapi);
        db.Open();
        return db;
    }

    private void Open()
    {
        try
        {
            // var dataPath = _sapi.GetOrCreateDataPath("woopessentials");
            // Directory.CreateDirectory(dataPath);

            var dbPath = Path.Combine(GamePaths.DataPath, "ModData", _sapi.WorldManager.SaveGame.SavegameIdentifier, "blockchanges.sqlite");

            var csb = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Default
            };

            _conn = new SqliteConnection(csb.ToString());
            _conn.Open();

            using (var pragma = _conn.CreateCommand())
            {
                // Performance oriented defaults for append-heavy workloads
                pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA temp_store=MEMORY; PRAGMA mmap_size=268435456;";
                pragma.ExecuteNonQuery();
            }

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = """

                                                  CREATE TABLE IF NOT EXISTS block_changes (
                                                      id          INTEGER PRIMARY KEY AUTOINCREMENT,
                                                      ts_ms       INTEGER NOT NULL, -- unix epoch ms
                                                      playeruid   TEXT,
                                                      action      INTEGER NOT NULL, -- 0=break, 1=place
                                                      x           INTEGER NOT NULL,
                                                      y           INTEGER NOT NULL,
                                                      z           INTEGER NOT NULL,
                                                      chunkx      INTEGER NOT NULL,
                                                      chunkz      INTEGER NOT NULL,
                                                      blockid  INTEGER
                                                  );
                                                  CREATE INDEX IF NOT EXISTS idx_block_changes_chunk ON block_changes(chunkx, chunkz);
                                                  CREATE INDEX IF NOT EXISTS idx_block_changes_player_ts ON block_changes(playeruid, ts_ms);
                                                  CREATE INDEX IF NOT EXISTS idx_block_changes_ts ON block_changes(ts_ms);
                                                  
                                  """;
                cmd.ExecuteNonQuery();
            }

            _insertCmd = _conn.CreateCommand();
            _insertCmd.CommandText = """

                                                     INSERT INTO block_changes (ts_ms, playeruid, action, x, y, z, chunkx, chunkz, blockid)
                                                     VALUES ($ts, $uid, $action, $x, $y, $z, $cx, $cz, $blockid);
                                                 
                                     """;
            _insertCmd.Parameters.Add("$ts", SqliteType.Integer);
            _insertCmd.Parameters.Add("$uid", SqliteType.Text);
            _insertCmd.Parameters.Add("$action", SqliteType.Integer);
            _insertCmd.Parameters.Add("$x", SqliteType.Integer);
            _insertCmd.Parameters.Add("$y", SqliteType.Integer);
            _insertCmd.Parameters.Add("$z", SqliteType.Integer);
            _insertCmd.Parameters.Add("$cx", SqliteType.Integer);
            _insertCmd.Parameters.Add("$cz", SqliteType.Integer);
            _insertCmd.Parameters.Add("$blockid", SqliteType.Integer);
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error("[WoopEssentials] Failed to open/create block change DB: {0}", ex);
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Logs a single block change. All parameters must be raw primitives to keep call overhead minimal.
    /// </summary>
    public void Log(long tsUnixMs, string? playerUid, int action, int x, int y, int z, int blockId)
    {
        if (_conn == null || _insertCmd == null) return;

        try
        {
            // Use bit shifts for chunk coordinates (assuming 16x16 columns)
            var cx = x >> 4;
            var cz = z >> 4;

            _insertCmd.Parameters["$ts"].Value = tsUnixMs;
            _insertCmd.Parameters["$uid"].Value = (object?)playerUid ?? DBNull.Value;
            _insertCmd.Parameters["$action"].Value = action;
            _insertCmd.Parameters["$x"].Value = x;
            _insertCmd.Parameters["$y"].Value = y;
            _insertCmd.Parameters["$z"].Value = z;
            _insertCmd.Parameters["$cx"].Value = cx;
            _insertCmd.Parameters["$cz"].Value = cz;
            _insertCmd.Parameters["$blockid"].Value = blockId == 0 ? DBNull.Value : blockId;

            _insertCmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            // Do not spam — log and continue. DB failures must not crash the server.
            _sapi.Logger.Error("[WoopEssentials] Block change log failed: {0}", ex);
        }
    }

    internal List<BlockChangeEvent> QueryAt(int x, int y, int z, int limit)
    {
        var results = new List<BlockChangeEvent>();
        if (_conn == null) return results;

        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                              SELECT ts_ms, playeruid, action, blockid
                                                              FROM block_changes
                                                              WHERE x=$x AND y=$y AND z=$z
                                                              ORDER BY ts_ms DESC
                                                              LIMIT $lim;
                              """;
            cmd.Parameters.AddWithValue("$x", x);
            cmd.Parameters.AddWithValue("$y", y);
            cmd.Parameters.AddWithValue("$z", z);
            cmd.Parameters.AddWithValue("$lim", limit);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var ts = r.GetInt64(0);
                var uid = r.IsDBNull(1) ? null : r.GetString(1);
                var action = r.GetInt32(2);
                var blockId = r.IsDBNull(3) ? (int?)null : r.GetInt32(3);
                results.Add(new BlockChangeEvent(ts, uid, action, blockId));
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error("[WoopEssentials] QueryAt failed: {0}", ex);
        }

        return results;
    }

    public void Dispose()
    {
        try { _insertCmd?.Dispose(); } catch { /* ignore */ }
        try { _conn?.Dispose(); } catch { /* ignore */ }
        _insertCmd = null;
        _conn = null;
    }
}
