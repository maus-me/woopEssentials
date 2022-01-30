using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        public static Th3Influxdb Instance;

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
                    {
                        WritePoint(PointData.Measurement("errors").Field("value", string.Format(message, args)));
                        break;
                    }
                case EnumLogType.Fatal:
                    break;
                case EnumLogType.Audit:
                    break;
                default:
                    break;
            }
        }

        private void WriteData(float t1)
        {
            _data = new List<PointData>();

            foreach (IServerPlayer player in _sapi.World.AllOnlinePlayers)
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
            public static bool Prefix(FrameProfilerUtil __instance, Stopwatch ___stopwatch, ref Dictionary<string, long> ___elems, long ___start)
            {
                if (!__instance.Enabled && !__instance.PrintSlowTicks)
                {
                    return false;
                }

                if (__instance.PrintSlowTicks)
                {
                    long total = 0;
                    foreach (KeyValuePair<string, long> val in ___elems)
                    {
                        total += val.Value;
                    }

                    double ms = (double)total / Stopwatch.Frequency * 1000;

                    if (ms > __instance.PrintSlowTicksThreshold)
                    {
                        List<KeyValuePair<string, long>> myList = ___elems.ToList();
                        myList.Sort((x, y) => y.Value.CompareTo(x.Value));
                        for (int i = 0; i < Math.Min(myList.Count, 8); i++)
                        {
                            KeyValuePair<string, long> val = myList[i];
                            if (val.Value > Instance?._config.InlfuxDBLogtickThreshold)
                            {
                                Instance?.WritePoint(PointData.Measurement("logticks").Tag("system", val.Key).Field("value", (double)val.Value / Stopwatch.Frequency * 1000.0));
                            }
                        }
                    }
                }
                long ticks = ___stopwatch.ElapsedTicks;
                ___elems["PrefixEnd"] = ticks - ___start;
                ___start = ticks;
                if ((bool)(Instance?._config.InlfuxDBOverwriteLogTicks))
                {
                    ___elems = new Dictionary<string, long>();
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}