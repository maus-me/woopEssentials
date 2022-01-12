
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
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
    internal class Th3TraceListener : TraceListener
    {
      private readonly ICoreServerAPI _api;

      public Th3TraceListener(ICoreServerAPI api)
      {
        _api = api;
      }
      public override void Write(string message)
      {
        WriteLine(message);
      }

      public override void WriteLine(string message)
      {
        _api.Logger.VerboseDebug($"[Influx] {message}");
      }
    }

    private Harmony harmony;

    public long WriteDataListenerID;

    public static Th3Influxdb Instance;

    private readonly string harmonyPatchkey = "Th3Essentials.InfluxDB.Patch";

    private InfluxDBClient client;

    private WriteApi writeApi;

    private ICoreServerAPI _api;

    private Th3Config _config;

    private ServerMain server;
    private Process VSProcess;

    internal void Init(ICoreServerAPI api)
    {
      harmony = new Harmony(harmonyPatchkey);
      harmony.PatchAll();

      _api = api;
      _config = Th3Essentials.Config;
      server = (ServerMain)_api.World;
      VSProcess = Process.GetCurrentProcess();

      client = InfluxDBClientFactory.Create(_config.InfluxConfig.InlfuxDBURL, _config.InfluxConfig.InlfuxDBToken);
      writeApi = client.GetWriteApi();
      if (_config.InfluxConfig.Debug)
      {
        client.SetLogLevel(LogLevel.Basic);
        _ = Trace.Listeners.Add(new Th3TraceListener(_api));
      }

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
            WriteRecord($"warnings value=\"{msg}\"");
            break;
          }
        case EnumLogType.Error:
          {
            string msg = string.Format(message, args);
            WriteRecord($"errors value=\"{msg}\"");
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

      WriteRecord($"clients value={server.Clients.Count}");

      int activeEntities = 0;
      foreach (KeyValuePair<long, Entity> loadedEntity in _api.World.LoadedEntities)
      {
        if (loadedEntity.Value.State != EnumEntityState.Inactive)
        {
          activeEntities++;
        }
      }
      WriteRecord($"entitiesActive value={activeEntities}");

      StatsCollection statsCollection = server.StatsCollector[GameMath.Mod(server.StatsCollectorIndex - 1, server.StatsCollector.Length)];
      if (statsCollection.ticksTotal > 0)
      {
        WriteRecord($"l2avgticktime value={statsCollection.tickTimeTotal / statsCollection.ticksTotal}");
        WriteRecord($"l2stickspersec value={statsCollection.ticksTotal / 2.0}");
      }
      WriteRecord($"packetspresec value={statsCollection.statTotalPackets / 2.0}");
      WriteRecord($"kilobytespersec value={decimal.Round((decimal)(statsCollection.statTotalPacketsLength / 2048.0), 2, MidpointRounding.AwayFromZero)}");

      VSProcess.Refresh();
      long memory = VSProcess.PrivateMemorySize64 / 1048576;
      WriteRecord($"memory value={memory}");

      WriteRecord($"threads value={server.Serverthreads.Count}");

      WriteRecord($"chunks value={_api.World.LoadedChunkIndices.Count()}");

      WriteRecord($"entities value={_api.World.LoadedEntities.Count()}");

      WriteRecord($"generatingChunks value={_api.WorldManager.CurrentGeneratingChunkCount}");
    }

    private void WriteRecord(string data, WritePrecision precision = WritePrecision.S)
    {
      if (!writeApi.Disposed)
      {
        writeApi.WriteRecord(_config.InfluxConfig.InlfuxDBBucket, _config.InfluxConfig.InlfuxDBOrg, precision, data);
      }
    }

    private void PlayerDisconnect(IServerPlayer byPlayer)
    {
      WriteRecord($"online,player=\"{byPlayer.PlayerName}\" isOn=false");
    }

    internal void PlayerDied(IServerPlayer byPlayer, string msg)
    {
      WriteRecord($"deaths,player=\"{byPlayer.PlayerName}\" value=\"{msg}\"");
    }

    private void PlayerNowPlaying(IServerPlayer byPlayer)
    {
      WriteRecord($"online,player=\"{byPlayer.PlayerName}\" isOn=true");
    }

    private void Shutdown()
    {
      Dispose();
    }

    public void Dispose()
    {
      _api.Event.PlayerNowPlaying -= PlayerNowPlaying;
      _api.Event.PlayerDisconnect -= PlayerDisconnect;
      _api.Logger.EntryAdded -= LogEntryAdded;

      _api.Event.UnregisterGameTickListener(WriteDataListenerID);

      writeApi.Dispose();
      client.Dispose();

      if (harmony != null)
      {
        harmony.UnpatchAll(harmonyPatchkey);
      }
    }

    [HarmonyPatch(typeof(FrameProfilerUtil), nameof(FrameProfilerUtil.End))]
    public class PatchFrameProfilerUtil
    {
      static bool Prefix(FrameProfilerUtil __instance, Stopwatch ___stopwatch, ref Dictionary<string, long> ___elems, long ___start)
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
                Instance.WriteRecord($"logticks,system=\"{val.Key}\" value={(double)val.Value / Stopwatch.Frequency * 1000.0}", WritePrecision.Ms);
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

    [HarmonyPatch(typeof(Vintagestory.API.Common.Block), nameof(Vintagestory.API.Common.Block.OnBlockInteractStart))]
    public class PatchBlock
    {
      public static void Postfix(ref bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
      {
        if (!__result)
        {
          (world.Api as IServerAPI).LogVerboseDebug($"{byPlayer.PlayerName} {blockSel.Position}");
        }
      }
    }
  }
}