using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Th3Essentials.Config;
using Th3Essentials.Discord;
using Th3Essentials.InfluxDB;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

namespace Th3Essentials.Influxdb
{
    internal class Th3Influxdb
    {
        private Harmony _harmony;

        private long _writeDataListenerId;

        public static Th3Influxdb? Instance { get; set; }

        private const string HarmonyPatchKey = "Th3Essentials.InfluxDB.Patch";

        private InfluxDbClient _client;

        private ICoreServerAPI _sapi;

        private Th3InfluxConfig _config;

        private ServerMain _server;

        private Process _vsProcess;

        private List<PointData> _data;
        private List<PointData> _dataOnline;

        internal void Init(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _config = Th3Essentials.Config.InfluxConfig;
            _server = (ServerMain)_sapi.World;
            _vsProcess = Process.GetCurrentProcess();

            _client = new InfluxDbClient(_config.InlfuxDBURL, _config.InlfuxDBToken, _config.InlfuxDBOrg,
                _config.InlfuxDBBucket, sapi);
            if (!_client.HasConnection())
            {
                _client = null;
                return;
            }

            _harmony = new Harmony(HarmonyPatchKey);
            var original = typeof(FrameProfilerUtil).GetMethod(nameof(FrameProfilerUtil.End));
            var prefix =
                new HarmonyMethod(typeof(PatchFrameProfilerUtil).GetMethod(nameof(PatchFrameProfilerUtil.Prefix)));
            _harmony.Patch(original, prefix: prefix);

            var original2 = typeof(ChatCommandApi).GetMethods().First(m =>
                m.Name.Equals("Execute") &&
                m.GetParameters().Any(p => p.ParameterType.IsAssignableFrom(typeof(IServerPlayer))));
            var prefix2 =
                new HarmonyMethod(typeof(PatchAdminLogging).GetMethod(nameof(PatchAdminLogging.TriggerChatCommand)));
            _harmony.Patch(original2, prefix: prefix2);

            var original4 = typeof(InventoryPlayerCreative).GetMethod(nameof(InventoryPlayerCreative.ActivateSlot));
            var postfix4 =
                new HarmonyMethod(typeof(PatchAdminLogging).GetMethod(nameof(PatchAdminLogging.ActivateSlot)));
            _harmony.Patch(original4, postfix: postfix4);

            var handleCreateItemstack = typeof(ServerMain).Assembly.GetType("Vintagestory.Server.ServerSystemInventory")
                .GetMethod("HandleCreateItemstack", BindingFlags.NonPublic | BindingFlags.Instance);
            var handleCreateItemstackPostfix =
                new HarmonyMethod(typeof(PatchAdminLogging).GetMethod(nameof(PatchAdminLogging.HandleCreateItemstack)));

            _harmony.Patch(handleCreateItemstack, postfix: handleCreateItemstackPostfix);

            _sapi.Logger.EntryAdded += LogEntryAdded;
            _sapi.Event.DidPlaceBlock += OnDidPlaceBlock;
            _sapi.Event.DidBreakBlock += OnDidBreakBlock;

            _writeDataListenerId = _sapi.Event.RegisterGameTickListener(WriteOnline, 10000);
            _writeDataListenerId = _sapi.Event.RegisterGameTickListener(WriteData, _config.DataCollectInterval);
            Instance = this;
        }

        private void OnDidBreakBlock(IServerPlayer byPlayer, int oldBlockId, BlockSelection blockSel)
        {
            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) return;
            var pointData = PointData.Measurement("playerlogcrbreak").Tag("player", byPlayer.PlayerName.ToLower())
                .Tag("playerUID", byPlayer.PlayerUID).Tag("position", blockSel.Position.ToString()).Field("value",
                    $"{Instance._sapi.World.Blocks[oldBlockId].Code} {blockSel.Position}");
            WritePoint(pointData, WritePrecision.Ms);
        }

        private void OnDidPlaceBlock(IServerPlayer byPlayer, int oldBlockId, BlockSelection blockSel,
            ItemStack withItemStack)
        {
            if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative) return;
            var pointData = PointData.Measurement("playerlogcrplace").Tag("player", byPlayer.PlayerName.ToLower())
                .Tag("playerUID", byPlayer.PlayerUID).Tag("position", blockSel.Position.ToString()).Field("value",
                    $"{withItemStack.Collectible?.Code} {blockSel.Position}");
            WritePoint(pointData, WritePrecision.Ms);
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
                    var msg = string.Format(message, args);
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
            }
        }

        private void WriteData(float t1)
        {
            _data = new List<PointData>();

            var activeEntities =
                _sapi.World.LoadedEntities.Count(loadedEntity => loadedEntity.Value.State != EnumEntityState.Inactive);
            _data.Add(PointData.Measurement("entitiesActive").Field("value", activeEntities));


            var statsCollection =
                _server.StatsCollector[GameMath.Mod(_server.StatsCollectorIndex - 1, _server.StatsCollector.Length)];
            if (statsCollection.ticksTotal > 0)
            {
                _data.Add(PointData.Measurement("l2avgticktime").Field("value",
                    (double)statsCollection.tickTimeTotal / statsCollection.ticksTotal));
                _data.Add(PointData.Measurement("l2stickspersec").Field("value", statsCollection.ticksTotal / 2.0));
            }

            _data.Add(PointData.Measurement("packetspresec").Field("value", statsCollection.statTotalPackets / 2.0));
            _data.Add(PointData.Measurement("kilobytespersec").Field("value",
                decimal.Round((decimal)(statsCollection.statTotalPacketsLength / 2048.0), 2,
                    MidpointRounding.AwayFromZero)));

            _vsProcess.Refresh();
            var memory = _vsProcess.PrivateMemorySize64 / 1048576;
            _data.Add(PointData.Measurement("memory").Field("value", memory));

            _data.Add(PointData.Measurement("threads").Field("value", _server.Serverthreads.Count));

            _data.Add(PointData.Measurement("chunks").Field("value", _sapi.World.LoadedChunkIndices.Length));

            _data.Add(PointData.Measurement("entities").Field("value", _sapi.World.LoadedEntities.Count));

            _data.Add(PointData.Measurement("generatingChunks")
                .Field("value", _sapi.WorldManager.CurrentGeneratingChunkCount));
            WritePoints(_data);
        }

        private void WriteOnline(float t1)
        {
            _dataOnline = new List<PointData>();

            foreach (var player in _sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
            {
                if (player.ConnectionState == EnumClientState.Playing)
                {
                    _dataOnline.Add(PointData.Measurement("online").Tag("player", player.PlayerName.ToLower())
                        .Field("value", player.Ping));
                }
            }

            _dataOnline.Add(PointData.Measurement("clients").Field("value", _server.Clients.Count));
            WritePoints(_dataOnline);
        }

        private void WritePoints(List<PointData> data, WritePrecision? precision = null)
        {
            _client?.WritePoints(data, precision);
        }

        private void WritePoint(PointData data, WritePrecision? precision = null)
        {
            _client?.WritePoint(data, precision);
        }

        internal void PlayerDied(IServerPlayer byPlayer, string msg)
        {
            WritePoint(PointData.Measurement("deaths").Tag("player", byPlayer.PlayerName.ToLower())
                .Field("value", msg));
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _sapi.Logger.EntryAdded -= LogEntryAdded;

                _sapi.Event.UnregisterGameTickListener(_writeDataListenerId);

                _client.Dispose();
            }

            _harmony?.UnpatchAll(HarmonyPatchKey);
        }

        public class PatchAdminLogging
        {
            public static void TriggerChatCommand(string commandName, IServerPlayer player, int groupId, string args,
                Action<TextCommandResult> onCommandComplete)
            {
                var pointData = PointData.Measurement("playerlog").Tag("player", player.PlayerName.ToLower())
                    .Tag("playerUID", player.PlayerUID).Field("value", $"{commandName} {args}");
                Instance?.WritePoint(pointData);
                try
                {
                    
                    var cmd = Instance?._sapi.ChatCommands.Get(commandName) as ChatCommandImpl;
                    var priv = cmd?.GetType().GetField("privilege", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(cmd);
                    
                    if (Th3Essentials.Config.DiscordConfig.AdminPrivilegeToMonitor?.Contains(priv) == true)
                    {
                            Th3Discord.Instance.SendAdminLog($"**{player.PlayerName}** executed: {commandName} {args}");
                    }
                    else if(Equals("gamemode", commandName)) { 
                        Th3Discord.Instance.SendAdminLog($"**{player.PlayerName}** executed @ ({player.Entity.Pos.AsBlockPos}): {commandName} {args}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e); // TODO
                }
            }

            public static void ActivateSlot(InventoryPlayerCreative __instance, int slotId, ItemSlot sourceSlot,
                ref ItemStackMoveOperation op)
            {
                if (op.MovedQuantity == 0) return;
                if (op.ShiftDown)
                {
                    var itemSlot = __instance[slotId];
                    var pointData = PointData.Measurement("playerloginv")
                        .Tag("player", op.ActingPlayer?.PlayerName.ToLower())
                        .Tag("playerUID", op.ActingPlayer?.PlayerUID).Field("value",
                            $"{op.MovedQuantity} {itemSlot.Itemstack?.Collectible?.Code}");
                    Instance?.WritePoint(pointData);
                    Th3Discord.Instance.SendAdminLog($"**{op.ActingPlayer?.PlayerName}** spawned: {op.MovedQuantity} {itemSlot.Itemstack?.Collectible?.Code}");
                }
                else
                {
                    var pointData = PointData.Measurement("playerloginv")
                        .Tag("player", op.ActingPlayer?.PlayerName.ToLower())
                        .Tag("playerUID", op.ActingPlayer?.PlayerUID).Field("value",
                            $"{op.MovedQuantity} {sourceSlot.Itemstack?.Collectible?.Code}");
                    Instance?.WritePoint(pointData);
                    Th3Discord.Instance.SendAdminLog($"**{op.ActingPlayer?.PlayerName}** spawned: {op.MovedQuantity} {sourceSlot.Itemstack?.Collectible?.Code}");
                }
            }

            public static void HandleCreateItemstack(Packet_Client packet, ConnectedClient client)
            {
                ServerPlayer player = (ServerPlayer)client.GetType()
                    .GetField("Player", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(client);
                Packet_CreateItemstack createpacket = (Packet_CreateItemstack)packet.GetType()
                    .GetField("CreateItemstack", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(packet);
                string targetInventoryId = (string)createpacket.GetType()
                    .GetField("TargetInventoryId", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(createpacket);
                int targetSlot = (int)createpacket.GetType()
                    .GetField("TargetSlot", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(createpacket);

                player.InventoryManager.GetInventory(targetInventoryId, out var inv);
                ItemSlot slot = inv?[targetSlot];

                if (player.WorldData.CurrentGameMode == EnumGameMode.Creative && slot.Itemstack != null)
                {
                    var pointData = PointData.Measurement("playerloginv")
                        .Tag("player", player.PlayerName.ToLower())
                        .Tag("playerUID", player.PlayerUID).Field("value",
                            $"1 {slot.Itemstack?.Collectible?.Code}");
                    Instance?.WritePoint(pointData);
                    Th3Discord.Instance.SendAdminLog($"**{player.PlayerName}** spawned: 1 {slot.Itemstack?.Collectible?.Code}");
                }
            }
        }

        public class PatchFrameProfilerUtil
        {
            public static bool Prefix(FrameProfilerUtil __instance, ProfileEntryRange ___rootEntry, Action<string> ___onLogoutputHandler)
            {
                if (!__instance.Enabled && !__instance.PrintSlowTicks)
                {
                    return false;
                }

                __instance.Mark("prefixEnd");
                __instance.Leave();

                __instance.PrevRootEntry = ___rootEntry;

                var ms = (double)___rootEntry.ElapsedTicks / Stopwatch.Frequency * 1000;

                if (!__instance.PrintSlowTicks || !(ms > __instance.PrintSlowTicksThreshold)) return false;
                StringBuilder stringBuilder;
                stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"A tick took {ms:0.##} ms");

                SlowTicksToString(___rootEntry, stringBuilder);

                var message = stringBuilder.ToString();
                if (Instance?._config.InlfuxDBOverwriteLogTicks == false)
                {
                    ___onLogoutputHandler(message);
                }

                var data = PointData.Measurement("logticks").Field("log", message).Field("ms", ms).Timestamp(WritePrecision.Ms);

                Instance?.WritePoint(data, WritePrecision.Ms);

                return false;
            }

            private static void SlowTicksToString(ProfileEntryRange entry, StringBuilder stringBuilder, double thresholdMs = 0.35, string indent = "")
            {
                var timeMs = (double)entry.ElapsedTicks / Stopwatch.Frequency * 1000;
                if (timeMs < thresholdMs)
                {
                    return;
                }

                if (entry.CallCount > 1)
                {
                    stringBuilder.AppendLine(
                        $"{indent}{timeMs:0.00}ms, {entry.CallCount:####} calls, avg {timeMs * 1000 / Math.Max(entry.CallCount, 1):0.00} us/call: {entry.Code}"
                    );
                }
                else
                {
                    stringBuilder.AppendLine(
                        $"{indent}{timeMs:0.00}ms, {entry.CallCount:####} call : {entry.Code}"
                    );
                }

                var profiles = new List<ProfileEntryRange>();

                if (entry.Marks != null)
                {
                    profiles.AddRange(entry.Marks.Select(e => new ProfileEntryRange()
                        { ElapsedTicks = e.Value.ElapsedTicks, Code = e.Key, CallCount = e.Value.CallCount }));
                }

                if (entry.ChildRanges != null)
                {
                    profiles.AddRange(entry.ChildRanges.Values);
                }

                var orderByDescending = profiles.OrderByDescending((prof) => prof.ElapsedTicks);

                var i = 0;
                foreach (var prof in orderByDescending)
                {
                    if (i++ > 8)
                    {
                        return;
                    }

                    SlowTicksToString(prof, stringBuilder, thresholdMs, indent + "  ");
                }
            }
        }
    }
}