using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Th3Essentials.Commands;
using Th3Essentials.Config;
using Th3Essentials.Discord;
using Th3Essentials.Influxdb;
using Th3Essentials.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

[assembly: ModInfo("Th3Essentials",
    Description = "Th3Dilli essentials server mod",
    Website = "https://gitlab.com/Th3Dilli/",
    Authors = new[] { "Th3Dilli" })]
namespace Th3Essentials
{

    public delegate void PlayerWithRewardJoin(IServerPlayer player, string discordRewardId);
    
    public class Th3Essentials : ModSystem
    {
        private const string _configFile = "Th3Config.json";

        internal static Th3Config Config { get; private set; }

        internal static Th3PlayerConfig PlayerConfig { get; private set; }

        internal static string Th3EssentialsModDataKey = "Th3Essentials";

        internal ICoreServerAPI _sapi;

        private Th3Discord _th3Discord;

        private Th3Influxdb _th3Influx;

        public event PlayerWithRewardJoin OnPlayerWithRewardJoin;

        public Th3Essentials()
        {
        }

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            try
            {
                Config = _sapi.LoadModConfig<Th3Config>(_configFile);

                if (Config == null)
                {
                    Config = new Th3Config();
                    Config.Init();
                    _sapi.StoreModConfig(Config, _configFile);

                    _sapi.Server.LogWarning(Lang.Get("th3essentials:config-init"));
                    _sapi.Server.LogWarning(Lang.Get("th3essentials:config-file-info", Path.Combine(GamePaths.ModConfig, _configFile)));
                }
            }
            catch (Exception e)
            {
                _sapi.Logger.Error(Lang.Get("th3essentials:th3config-error", e));
                _sapi.Logger.Error(Lang.Get("th3essentials:disabled"));
                return;
            }

            PlayerConfig = new Th3PlayerConfig();

            _sapi.Event.GameWorldSave += GameWorldSave;
            _sapi.Event.PlayerNowPlaying += PlayerNowPlaying;

            if (Config.IsShutdownConfigured())
            {
                if (Config.ShutdownTimes?.Length > 0)
                {
                    var next = TimeSpan.Zero;
                    var nextMin = double.MaxValue;
                    foreach (var time in Config.ShutdownTimes)
                    {
                        var timeMin = Th3Util.GetTimeTillRestart(time);
                        if (timeMin.TotalMinutes < nextMin)
                        {
                            nextMin = timeMin.TotalMinutes;
                            next = time;
                        }
                    }
                    Config.ShutdownTime = next;
                }
                _ = _sapi.Event.RegisterGameTickListener(CheckRestart, 60000);
            }

            CommandsLoader.Init(_sapi);
            new Homesystem().Init(_sapi);
            new Starterkitsystem().Init(_sapi);
            new Announcementsystem().Init(_sapi);

            if (Config.IsDiscordConfigured())
            {
                _th3Discord = new Th3Discord();
                _th3Discord.Init(this);
            }
            else
            {
                // enable show role here when discord is not active - else it is enabled in the Th3Discord
                if (Config.ShowRole)
                {
                    _sapi.Event.PlayerChat += PlayerChatAsync;
                }
                _sapi.Logger.Debug("Discordbot needs to be configured, functionality disabled!!!");
            }

            if (Config.IsInlfuxDBConfigured())
            {
                _th3Influx = new Th3Influxdb();
                _th3Influx.Init(_sapi);
            }

            if (Config.IsInlfuxDBConfigured() || Config.IsDiscordConfigured())
            {
                _sapi.Event.PlayerDeath += PlayerDeathAsync;
            }

            if (Config.AdminRoles?.Count > 0)
            {
                _ = _sapi.RegisterCommand("admins", Lang.Get("th3essentials:slc-admins"), string.Empty,
                 (IServerPlayer player, int groupId, CmdArgs args) =>
                 {
                     player.SendMessage(GlobalConstants.GeneralChatGroup, Th3Util.GetAdmins(_sapi), EnumChatType.CommandSuccess);
                 }, Privilege.chat);
            }

            _ = _sapi.RegisterCommand("reloadth3config", Lang.Get("th3essentials:cd-reloadConfig"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    if (ReloadConfig())
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-reloadconfig-msg"), EnumChatType.CommandSuccess);
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-reloadconfig-fail"), EnumChatType.CommandError);
                    }
                }, Privilege.controlserver);
        }

        internal void PlayerWithRewardJoin(IServerPlayer player, string discordRewardId){
            OnPlayerWithRewardJoin?.Invoke(player, discordRewardId);
        }

        private void PlayerChatAsync(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
        {
            message = string.Format(Config.RoleFormat, ToHex(byPlayer.Role.Color), byPlayer.Role.Name, message);
        }

        private void CheckRestart(float t1)
        {
            int TimeInMinutes = (int)Th3Util.GetTimeTillRestart(Config.ShutdownTime).TotalMinutes;
            if (Config.ShutdownAnnounce != null)
            {
                foreach (int time in Config.ShutdownAnnounce)
                {
                    if (time == TimeInMinutes)
                    {
                        string msg = TimeInMinutes == 1 ? Lang.Get("th3essentials:restart-in-min") : Lang.Get("th3essentials:restart-in-mins", TimeInMinutes);
                        _sapi.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
                        _th3Discord?.SendServerMessage(msg);
                        _sapi.Logger.Event(msg);
                    }
                }
            }
            if (Config.ShutdownEnabled && TimeInMinutes < 1)
            {
                if (Config.BackupOnShutdown)
                {
                    LockAndKick();
                    CreateBackup();
                }
                _sapi.Server.ShutDown();
            }
        }

        private void CreateBackup()
        {
            ServerMain server = (ServerMain)_sapi.World;
            GameDatabase gameDatabase = new GameDatabase(_sapi.Logger);

            _ = gameDatabase.ProbeOpenConnection(server.GetSaveFilename(), true, out _, out _, out _);
            FileInfo fileInfo = new FileInfo(gameDatabase.DatabaseFilename);
            long freeDiskSpace = ServerMain.xPlatInterface.GetFreeDiskSpace(fileInfo.DirectoryName);
            if (freeDiskSpace <= fileInfo.Length)
            {
                _sapi.Logger.Warning($"SaveFileSize: {fileInfo.Length / 1000000} MB, FreeDiskSpace: {freeDiskSpace / 1000000} MB");
                _sapi.Logger.Error("Not enought disk space left to create a backup");
                return;
            }

            string worldName = Path.GetFileNameWithoutExtension(_sapi.WorldManager.CurrentWorldName);
            if (worldName.Length == 0)
            {
                worldName = "world";
            }
            string backupFileName = $"{worldName}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.vcdbs";

            _sapi.Logger.Event(Lang.Get("th3essentials:backup"));
            _th3Discord?.SendServerMessage(Lang.Get("th3essentials:backup-dc"));

            gameDatabase.CreateBackup(backupFileName);
        }

        private void LockAndKick()
        {
            _sapi.Server.Config.Password = new Random().Next().ToString();
            _sapi.Logger.Event($"Temporary server password is: {_sapi.Server.Config.Password}");
            foreach (IServerPlayer player in _sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
            {
                player.Disconnect();
            }
        }

        private void PlayerNowPlaying(IServerPlayer byPlayer)
        {
            if (!PlayerConfig.Players.TryGetValue(byPlayer.PlayerUID, out _))
            {
                byte[] data = byPlayer.WorldData.GetModdata(Th3EssentialsModDataKey);
                if (data != null)
                {
                    Th3PlayerData playerData = SerializerUtil.Deserialize<Th3PlayerData>(data);
                    PlayerConfig.Add(byPlayer.PlayerUID, playerData);
                }
            }
        }

        private void PlayerDeathAsync(IServerPlayer byPlayer, DamageSource damageSource)
        {
            string msg;
            if (damageSource != null)
            {
                string key = null;
                int numMax = 1;
                if (damageSource.SourceEntity != null)
                {
                    key = damageSource.SourceEntity.Code.Path.Replace("-", "");
                    if (key.Contains("wolf"))
                    {
                        numMax = 4;
                    }
                    else if (key.Contains("pig"))
                    {
                        numMax = 1;
                    }
                    else if (key.Contains("drifter"))
                    {
                        numMax = 3;
                    }
                    else if (key.Contains("sheep"))
                    {
                        if (key.Contains("female"))
                        {
                            key = "sheepbighornmale";
                        }
                        numMax = 3;
                    }
                    else if (key.Contains("locust"))
                    {
                        numMax = 2;
                    }
                }
                else
                {
                    if (damageSource.Source == EnumDamageSource.Explosion)
                    {
                        key = "explosion";
                        numMax = 4;
                    }
                    else if (damageSource.Type == EnumDamageType.Hunger)
                    {
                        key = "hunger";
                        numMax = 3;
                    }
                    else if (damageSource.Type == EnumDamageType.Fire)
                    {
                        key = "fire-block";
                        numMax = 3;
                    }
                    else if (damageSource.Source == EnumDamageSource.Fall)
                    {
                        key = "fall";
                        numMax = 4;
                    }
                }

                if (key != null)
                {
                    Random rnd = new Random();

                    msg = Lang.Get("deathmsg-" + key + "-" + rnd.Next(1, numMax), byPlayer.PlayerName);
                    if (msg.Contains("deathmsg"))
                    {
                        string str = Lang.Get("prefixandcreature-" + key);
                        msg = Lang.Get("th3essentials:playerdeathby", byPlayer.PlayerName, str);
                    }
                }
                else
                {
                    msg = Lang.Get("th3essentials:playerdeath", byPlayer.PlayerName);
                }
            }
            else
            {
                msg = Lang.Get("th3essentials:playerdeath", byPlayer.PlayerName);
            }

            Th3Influxdb.Instance?.PlayerDied(byPlayer, msg);
            _th3Discord?.SendServerMessage(msg);
        }

        private void GameWorldSave()
        {
            if (Config != null && Config.IsDirty)
            {
                Config.IsDirty = false;
                _sapi.StoreModConfig(Config, _configFile);
            }

            PlayerConfig.GameWorldSave(_sapi);
        }

        private bool ReloadConfig()
        {
            try
            {
                Th3Config configTemp = _sapi.LoadModConfig<Th3Config>(_configFile);
                Config.Reload(configTemp);
            }
            catch (Exception e)
            {
                _sapi.Logger.Error("Error reloading Th3Config: ", e.ToString());
                return false;
            }
            return true;
        }

        public override void Dispose()
        {
            _th3Influx?.Dispose();
            _th3Discord?.Dispose();
            base.Dispose();
        }

        public static string ToHex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }
}
