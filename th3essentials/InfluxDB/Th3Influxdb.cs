using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Th3Essentials.Config;
using Th3Essentials.InfluxDB;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Th3Essentials.Influxdb
{
    internal class Th3Influxdb
    {
        private Harmony _harmony;

        private long _writeDataListenerID;

        public static Th3Influxdb Instance { get; set; }

        private readonly string _harmonyPatchkey = "Th3Essentials.InfluxDB.Patch";

        private InfluxDBClient _client;

        private ICoreServerAPI _sapi;

        private Th3InfluxConfig _config;

        private ServerMain _server;

        private Process _vsProcess;

        private List<PointData> _data;

        internal void Init(ICoreServerAPI sapi)
        {
            _harmony = new Harmony(_harmonyPatchkey);
            MethodInfo original = typeof(FrameProfilerUtil).GetMethod(nameof(FrameProfilerUtil.End));
            HarmonyMethod prefix = new HarmonyMethod(typeof(PatchFrameProfilerUtil).GetMethod(nameof(PatchFrameProfilerUtil.Prefix)));
            _harmony.Patch(original, prefix: prefix);

            _sapi = sapi;
            _config = Th3Essentials.Config.InfluxConfig;
            _server = (ServerMain)_sapi.World;
            _vsProcess = Process.GetCurrentProcess();

            _client = new InfluxDBClient(_config.InlfuxDBURL, _config.InlfuxDBToken, _config.InlfuxDBOrg, _config.InlfuxDBBucket, sapi);

            _sapi.Logger.EntryAdded += LogEntryAdded;

            _writeDataListenerID = _sapi.Event.RegisterGameTickListener(WriteData, 10000);
            Instance = this;
        }

        private void LogEntryAdded(EnumLogType logType, string message, object[] args)
        {
            switch (logType)
            {
                case EnumLogType.Chat:
                    break;
                case EnumLogType.Event:
                    break;
                case EnumLogType.StoryEvent:
                    break;
                case EnumLogType.Build:
                    break;
                case EnumLogType.VerboseDebug:
                    break;
                case EnumLogType.Debug:
                    break;
                case EnumLogType.Notification:
                    break;
                case EnumLogType.Warning:
                    {
                        string msg = string.Format(message, args);
                        if (msg.Contains("Server overloaded"))
                        {
                            WritePoint(PointData.Measurement("overloadwarnings").Field("value", msg));
                        }
                        else
                        {
                            WritePoint(PointData.Measurement("warnings").Field("value", msg));
                        }
                        break;
                    }
                case EnumLogType.Error:
                case EnumLogType.Fatal:
                    {
                        WritePoint(PointData.Measurement("errors").Field("value", string.Format(message, args)));
                        break;
                    }
                case EnumLogType.Audit:
                    break;
                default:
                    break;
            }
        }

        private void WriteData(float t1)
        {
            _data = new List<PointData>();

            foreach (IServerPlayer player in _sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
            {
                if (player.ConnectionState == EnumClientState.Playing)
                {
                    _data.Add(PointData.Measurement("online").Tag("player", player.PlayerName.ToLower()).Field("value", player.Ping));
                }
            }

            _data.Add(PointData.Measurement("clients").Field("value", _server.Clients.Count));


            int activeEntities = 0;
            foreach (KeyValuePair<long, Entity> loadedEntity in _sapi.World.LoadedEntities)
            {
                if (loadedEntity.Value.State != EnumEntityState.Inactive)
                {
                    activeEntities++;
                }
            }
            _data.Add(PointData.Measurement("entitiesActive").Field("value", activeEntities));


            StatsCollection statsCollection = _server.StatsCollector[GameMath.Mod(_server.StatsCollectorIndex - 1, _server.StatsCollector.Length)];
            if (statsCollection.ticksTotal > 0)
            {
                _data.Add(PointData.Measurement("l2avgticktime").Field("value", (double)statsCollection.tickTimeTotal / statsCollection.ticksTotal));
                _data.Add(PointData.Measurement("l2stickspersec").Field("value", statsCollection.ticksTotal / 2.0));
            }
            _data.Add(PointData.Measurement("packetspresec").Field("value", statsCollection.statTotalPackets / 2.0));
            _data.Add(PointData.Measurement("kilobytespersec").Field("value", decimal.Round((decimal)(statsCollection.statTotalPacketsLength / 2048.0), 2, MidpointRounding.AwayFromZero)));

            _vsProcess.Refresh();
            long memory = _vsProcess.PrivateMemorySize64 / 1048576;
            _data.Add(PointData.Measurement("memory").Field("value", memory));

            _data.Add(PointData.Measurement("threads").Field("value", _server.Serverthreads.Count));

            _data.Add(PointData.Measurement("chunks").Field("value", _sapi.World.LoadedChunkIndices.Count()));

            _data.Add(PointData.Measurement("entities").Field("value", _sapi.World.LoadedEntities.Count()));

            _data.Add(PointData.Measurement("generatingChunks").Field("value", _sapi.WorldManager.CurrentGeneratingChunkCount));
            WritePoints(_data);
        }

        private void WritePoints(List<PointData> data)
        {
            _client?.WritePoints(data);
        }

        private void WritePoint(PointData data)
        {
            _client?.WritePoint(data);
        }

        internal void PlayerDied(IServerPlayer byPlayer, string msg)
        {
            WritePoint(PointData.Measurement("deaths").Tag("player", byPlayer.PlayerName.ToLower()).Field("value", msg));
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _sapi.Logger.EntryAdded -= LogEntryAdded;

                _sapi.Event.UnregisterGameTickListener(_writeDataListenerID);

                _client.Dispose();
            }

            _harmony?.UnpatchAll(_harmonyPatchkey);
        }

        public class PatchFrameProfilerUtil
        {
            public static bool Prefix(FrameProfilerUtil __instance, ProfileEntryRange ___rootEntry, ILogger ___logger)
            {
                if (!__instance.Enabled && !__instance.PrintSlowTicks)
                {
                    return false;
                }

                __instance.Mark("prefixEnd");
                __instance.Leave();

                __instance.PrevRootEntry = ___rootEntry;

                double ms = (double)___rootEntry.ElapsedTicks / Stopwatch.Frequency * 1000;
                if (__instance.PrintSlowTicks && ms > __instance.PrintSlowTicksThreshold)
                {
                    StringBuilder strib = null;
                    List<PointData> data = new List<PointData>();
                    if (!(bool)(Instance?._config.InlfuxDBOverwriteLogTicks))
                    {
                        strib = new StringBuilder();
                        strib.AppendLine(string.Format("A tick took {0:0.##} ms", ms));
                    }

                    SlowTicksToString(___rootEntry, strib, data);

                    if (!(bool)(Instance?._config.InlfuxDBOverwriteLogTicks))
                    {
                        ___logger.Notification(strib.ToString());
                    }
                    Instance?.WritePoints(data);
                }

                return false;
            }

            static void SlowTicksToString(ProfileEntryRange entry, StringBuilder strib, List<PointData> data, double thresholdMs = 0.35, string indent = "")
            {
                double timeMS = (double)entry.ElapsedTicks / Stopwatch.Frequency * 1000;
                if (timeMS < thresholdMs)
                {
                    return;
                }

                if (entry.CallCount > 1)
                {
                    if (!(bool)(Instance?._config.InlfuxDBOverwriteLogTicks))
                    {
                        strib.AppendLine(
                            indent + string.Format("{0:0.00}ms, {1:####} calls, avg {2:0.00} us/call: {3:0.00}",
                            timeMS, entry.CallCount, timeMS * 1000 / Math.Max(entry.CallCount, 1), entry.Code)
                        );
                    }
                    data.Add(PointData.Measurement("logticks").Tag("system", entry.Code).Field("value", timeMS).Field("call", entry.CallCount).Field("avg", timeMS / Math.Max(entry.CallCount, 1)));
                }
                else
                {
                    if (!(bool)(Instance?._config.InlfuxDBOverwriteLogTicks))
                    {
                        strib.AppendLine(
                            indent + string.Format("{0:0.00}ms, {1:####} call : {2}",
                            timeMS, entry.CallCount, entry.Code)
                        );
                    }
                    data.Add(PointData.Measurement("logticks").Tag("system", entry.Code).Field("value", timeMS).Field("call", entry.CallCount));
                }

                List<ProfileEntryRange> profiles = new List<ProfileEntryRange>();

                if (entry.Marks != null)
                {
                    profiles.AddRange(entry.Marks.Select(e => new ProfileEntryRange() { ElapsedTicks = e.Value.ElapsedTicks, Code = e.Key, CallCount = e.Value.CallCount }));
                }

                if (entry.ChildRanges != null)
                {
                    profiles.AddRange(entry.ChildRanges.Values);
                }

                IOrderedEnumerable<ProfileEntryRange> profsordered = profiles.OrderByDescending((prof) => prof.ElapsedTicks);

                int i = 0;
                foreach (ProfileEntryRange prof in profsordered)
                {
                    if (i++ > 8)
                    {
                        return;
                    }

                    SlowTicksToString(prof, strib, data, thresholdMs, indent + "  ");
                }
            }
        }
    }
}