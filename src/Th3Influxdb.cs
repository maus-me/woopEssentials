using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using InfluxDB;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Th3Essentials.Influxdb
{
    internal class Th3Influxdb
    {
        private Harmony harmony;

        public long WriteDataListenerID;

        public static Th3Influxdb Instance;

        private readonly string harmonyPatchkey = "Th3Essentials.InfluxDB.Patch";

        private InfluxDBClient client;

        private ICoreServerAPI _api;

        private Th3Config _config;

        private ServerMain server;

        private Process VSProcess;

        private List<PointData> data;

        internal void Init(ICoreServerAPI api)
        {
            harmony = new Harmony(harmonyPatchkey);
            harmony.PatchAll();
            _api = api;
            _config = Th3Essentials.Config;
            server = (ServerMain)_api.World;
            VSProcess = Process.GetCurrentProcess();
            data = new List<PointData>();

            client = new InfluxDBClient(_config.InfluxConfig.InlfuxDBURL, _config.InfluxConfig.InlfuxDBToken, api);

            _api.Event.PlayerNowPlaying += PlayerNowPlaying;
            _api.Event.PlayerDisconnect += PlayerDisconnect;
            _api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);
            _api.Logger.EntryAdded += LogEntryAdded;

            WriteDataListenerID = _api.Event.RegisterGameTickListener(WriteData, 10000);
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
            data.Add(PointData.Measurement("clients").Field("value", server.Clients.Count));

            int activeEntities = 0;
            foreach (KeyValuePair<long, Entity> loadedEntity in _api.World.LoadedEntities)
            {
                if (loadedEntity.Value.State != EnumEntityState.Inactive)
                {
                    activeEntities++;
                }
            }
            data.Add(PointData.Measurement("entitiesActive").Field("value", activeEntities));


            StatsCollection statsCollection = server.StatsCollector[GameMath.Mod(server.StatsCollectorIndex - 1, server.StatsCollector.Length)];
            if (statsCollection.ticksTotal > 0)
            {
                data.Add(PointData.Measurement("l2avgticktime").Field("value", (double)statsCollection.tickTimeTotal / statsCollection.ticksTotal));
                data.Add(PointData.Measurement("l2stickspersec").Field("value", statsCollection.ticksTotal / 2.0));
            }
            data.Add(PointData.Measurement("packetspresec").Field("value", statsCollection.statTotalPackets / 2.0));
            data.Add(PointData.Measurement("kilobytespersec").Field("value", decimal.Round((decimal)(statsCollection.statTotalPacketsLength / 2048.0), 2, MidpointRounding.AwayFromZero)));

            VSProcess.Refresh();
            long memory = VSProcess.PrivateMemorySize64 / 1048576;
            data.Add(PointData.Measurement("memory").Field("value", memory));

            data.Add(PointData.Measurement("threads").Field("value", server.Serverthreads.Count));

            data.Add(PointData.Measurement("chunks").Field("value", _api.World.LoadedChunkIndices.Count()));

            data.Add(PointData.Measurement("entities").Field("value", _api.World.LoadedEntities.Count()));

            data.Add(PointData.Measurement("generatingChunks").Field("value", _api.WorldManager.CurrentGeneratingChunkCount));
            WritePoints(data);
            data.Clear();
        }

        private void WritePoints(List<PointData> data)
        {
            if (!client.Disposed)
            {
                client.WritePoints(_config.InfluxConfig.InlfuxDBBucket, _config.InfluxConfig.InlfuxDBOrg, data);
            }
        }

        private void WritePoint(PointData data)
        {
            if (!client.Disposed)
            {
                client.WritePoint(_config.InfluxConfig.InlfuxDBBucket, _config.InfluxConfig.InlfuxDBOrg, data);
            }
        }

        private void PlayerDisconnect(IServerPlayer byPlayer)
        {
            WritePoint(PointData.Measurement("online").Tag("player", byPlayer.PlayerName).Field("isOn", false));
        }

        internal void PlayerDied(IServerPlayer byPlayer, string msg)
        {
            WritePoint(PointData.Measurement("deaths").Tag("player", byPlayer.PlayerName).Field("value", msg));
        }

        private void PlayerNowPlaying(IServerPlayer byPlayer)
        {
            WritePoint(PointData.Measurement("online").Tag("player", byPlayer.PlayerName).Field("isOn", true));
        }

        private void Shutdown()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (client != null)
            {
                _api.Event.PlayerNowPlaying -= PlayerNowPlaying;
                _api.Event.PlayerDisconnect -= PlayerDisconnect;
                _api.Logger.EntryAdded -= LogEntryAdded;

                _api.Event.UnregisterGameTickListener(WriteDataListenerID);

                client.Dispose();
            }

            if (harmony != null)
            {
                harmony.UnpatchAll(harmonyPatchkey);
            }
        }

        [HarmonyPatch(typeof(FrameProfilerUtil), nameof(FrameProfilerUtil.End))]
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
                            if (val.Value > Instance._config.InfluxConfig.InlfuxDBLogtickThreshold)
                            {
                                Instance.WritePoint(PointData.Measurement("logticks").Tag("system", val.Key).Field("value", (double)val.Value / Stopwatch.Frequency * 1000.0));
                            }
                        }
                    }
                }
                long ticks = ___stopwatch.ElapsedTicks;
                ___elems["PrefixEnd"] = ticks - ___start;
                ___start = ticks;
                if (Instance._config.InfluxConfig.InlfuxDBOverwriteLogTicks)
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

        // [HarmonyPatch(typeof(Block), nameof(Block.OnBlockInteractStart))]
        // public class PatchBlock
        // {
        //     public static void Postfix(ref bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        //     {
        //         if (!__result)
        //         {
        //             (world.Api as IServerAPI).LogVerboseDebug($"{byPlayer.PlayerName} {blockSel.Position}");
        //         }
        //     }
        // }
    }
}