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
    Description = "Th3Dilli essentials server mod",
    Website = "https://gitlab.com/Th3Dilli/",
    Authors = new[] { "Th3Dilli" })]
namespace Th3Essentials
{
    public class Th3Essentials : ModSystem
    {
        private const string configFile = "Th3Config.json";

        private const string playerconfigFile = "Th3PlayerConfig.json";

        internal static Th3Config config { get; private set; }

        internal static Th3PlayerConfig playerConfig { get; private set; }

        private ICoreServerAPI api;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.api = api;

            api.Event.GameWorldSave += GameWorldSave;
            api.Event.PlayerNowPlaying += PlayerNowPlaying;

            config = api.LoadModConfig<Th3Config>(configFile);

            if (config == null)
            {
                config = new Th3Config();
                config.Init();
                api.StoreModConfig(config, configFile);
                api.Server.LogWarning(Lang.Get("th3essentials:config-init"));
                api.Server.LogWarning(Lang.Get("th3essentials:config-file-info", Path.Combine(GamePaths.ModConfig, configFile)));
            }

            Th3PlayerData.defaultHomeLimit = config.homeLimit;
            Th3PlayerData.defaultHomeCooldown = config.homeCooldown;

            playerConfig = api.LoadModConfig<Th3PlayerConfig>(playerconfigFile);

            if (playerConfig == null)
            {
                playerConfig = new Th3PlayerConfig();
                api.StoreModConfig(playerConfig, playerconfigFile);
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
            if (playerConfig.GetPlayerDataByUID(byPlayer.PlayerUID) == null)
            {
                Th3PlayerData playerData = new Th3PlayerData(byPlayer.PlayerUID);
                playerConfig.players.Add(playerData);
            }
        }

        private void GameWorldSave()
        {
            SaveConfig(api);
            SavePlayerConfig(api);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ReloadConfig()
        {
            Th3Config configTemp = api.LoadModConfig<Th3Config>(configFile);
            config.announcementInterval = configTemp.announcementInterval;
            config.announcementMessages.Clear();
            config.announcementMessages.AddRange(configTemp.announcementMessages);
            config.infoMessages.Clear();
            config.infoMessages.AddRange(configTemp.infoMessages);
            config.items.Clear();
            config.items.AddRange(configTemp.items);
            Th3PlayerConfig playerconfigTemp = api.LoadModConfig<Th3PlayerConfig>(playerconfigFile);
            playerConfig.players.Clear();
            playerConfig.players.AddRange(playerconfigTemp.players);
        }

        private void SaveConfig(ICoreServerAPI api)
        {
            api.StoreModConfig(config, configFile);
        }

        private void SavePlayerConfig(ICoreServerAPI api)
        {
            api.StoreModConfig(playerConfig, playerconfigFile);
        }
    }
}
