using System;
using System.IO;
using System.Runtime.CompilerServices;
using Th3Essentials.Announcements;
using Th3Essentials.Commands;
using Th3Essentials.Config;
using Th3Essentials.Homepoints;
using Th3Essentials.PlayerData;
using Th3Essentials.Starterkit;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

[assembly: ModInfo("Th3Essentials",
    Description = "Chill build survival essentials mod",
    Website = "https://gitlab.com/Th3Dilli/",
    Authors = new[] { "Th3Dilli" })]
namespace Th3Essentials
{
    public class Th3Essentials : ModSystem
    {
        private const string configFile = "Th3Config.json";

        private const string playerconfigFile = "Th3PlayerConfig.json";

        internal static Th3Config Config { get; private set; }

        internal static Th3PlayerConfig PlayerConfig { get; private set; }

        private ICoreServerAPI api;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.api = api;

            api.Event.GameWorldSave += GameWorldSave;
            api.Event.PlayerNowPlaying += PlayerNowPlaying;

            Config = api.LoadModConfig<Th3Config>(configFile);

            if (Config == null)
            {
                Config = new Th3Config();
                Config.Init();
                api.StoreModConfig(Config, configFile);
                api.Server.LogWarning(Lang.Get("th3essentials:config-init"));
                api.Server.LogWarning(Lang.Get("th3essentials:config-file-info", Path.Combine(GamePaths.ModConfig, configFile)));
            }

            PlayerConfig = api.LoadModConfig<Th3PlayerConfig>(playerconfigFile);
            Th3PlayerData.defaultHomeLimit = 10;
            Th3PlayerData.defaultHomeCooldown = 1;

            if (PlayerConfig == null)
            {
                PlayerConfig = new Th3PlayerConfig();
                api.StoreModConfig(PlayerConfig, playerconfigFile);
                api.Server.LogWarning(Lang.Get("th3essentials:playerconfig-init"));
                api.Server.LogWarning(Lang.Get("th3essentials:playerconfig-file-info", Path.Combine(GamePaths.ModConfig, playerconfigFile)));
            }

            api.Server.LogVerboseDebug($"DateTime: {DateTime.MinValue.Equals(new DateTime())}");

            CommandsLoader.Init(api);
            new Homesystem().Init(api);
            new Starterkitsystem().Init(api);
            new Announcementsystem().Init(api);

            api.RegisterCommand("reloadconfig", Lang.Get("th3essentials:cd-reloadConfig"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    ReloadConfig();
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-reloadconfig-msg"), EnumChatType.Notification);
                }, Privilege.controlserver);
        }

        private void PlayerNowPlaying(IServerPlayer byPlayer)
        {
            if (PlayerConfig.GetPlayerDataByUID(byPlayer.PlayerUID) == null)
            {
                Th3PlayerData playerData = new Th3PlayerData(byPlayer.PlayerUID);
                PlayerConfig.players.Add(playerData);
            }
        }

        private void GameWorldSave()
        {
            SaveConfig(api);
            SavePlayerConfig(api);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void ReloadConfig()
        {
            Th3Config configTemp = api.LoadModConfig<Th3Config>(configFile);
            Config.announcementInterval = configTemp.announcementInterval;
            Config.announcementMessages.Clear();
            Config.announcementMessages.AddRange(configTemp.announcementMessages);
            Config.infoMessages.Clear();
            Config.infoMessages.AddRange(configTemp.infoMessages);
            Config.items.Clear();
            Config.items.AddRange(configTemp.items);
            Th3PlayerConfig playerconfigTemp = api.LoadModConfig<Th3PlayerConfig>(playerconfigFile);
            PlayerConfig.players.Clear();
            PlayerConfig.players.AddRange(playerconfigTemp.players);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static void SaveConfig(ICoreServerAPI api)
        {
            api.StoreModConfig(Config, configFile);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static void SavePlayerConfig(ICoreServerAPI api)
        {
            api.StoreModConfig(PlayerConfig, playerconfigFile);
        }
    }
}
