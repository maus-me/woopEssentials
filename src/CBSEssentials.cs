using System;
using System.IO;
using System.Runtime.CompilerServices;
using CBSEssentials.Announcements;
using CBSEssentials.Commands;
using CBSEssentials.Config;
using CBSEssentials.Homepoints;
using CBSEssentials.Starterkit;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

[assembly: ModInfo("CBSEssentials",
    Description = "Chill build survival essentials mod",
    Website = "https://gitlab.com/Th3Dilli/",
    Authors = new[] { "Th3Dilli" })]
namespace CBSEssentials
{
    public class CBSEssentials : ModSystem
    {
        private const string configFile = "CBSConfig.json";

        private const string playerconfigFile = "CBSPlayerConfig.json";

        internal static CBSConfig config { get; private set; }

        internal static CBSPlayerConfig playerConfig { get; private set; }

        private ICoreServerAPI api;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.api = api;

            api.Event.GameWorldSave += GameWorldSave;

            config = api.LoadModConfig<CBSConfig>(configFile);

            if (config == null)
            {
                config = new CBSConfig();
                config.init();
                api.StoreModConfig(config, configFile);
                api.Server.LogWarning(Lang.Get("cbsessentials:config-init"));
                api.Server.LogWarning(Lang.Get("cbsessentials:config-file-info", Path.Combine(GamePaths.ModConfig, configFile)));
            }

            playerConfig = api.LoadModConfig<CBSPlayerConfig>(playerconfigFile);

            if (playerConfig == null)
            {
                playerConfig = new CBSPlayerConfig();
                api.StoreModConfig(playerConfig, playerconfigFile);
                api.Server.LogWarning(Lang.Get("cbsessentials:playerconfig-init"));
                api.Server.LogWarning(Lang.Get("cbsessentials:playerconfig-file-info", Path.Combine(GamePaths.ModConfig, playerconfigFile)));
            }

            api.Server.LogVerboseDebug($"DateTime: {DateTime.MinValue.Equals(new DateTime())}");

            CommandsLoader.init(api);
            new Homesystem().init(api);
            new Starterkitsystem().init(api);
            new Announcementsystem().init(api);

            api.RegisterCommand("reloadonfig", Lang.Get("cbsessentials:cd-reloadConfig"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    reloadConfig();
                }, Privilege.controlserver);
        }

        private void GameWorldSave()
        {
            saveConfig(api);
            savePlayerConfig(api);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void reloadConfig()
        {
            CBSConfig configTemp = api.LoadModConfig<CBSConfig>(configFile);
            config.announcementInterval = configTemp.announcementInterval;
            config.announcementMessages.Clear();
            config.announcementMessages.AddRange(configTemp.announcementMessages);
            config.infoMessages.Clear();
            config.infoMessages.AddRange(configTemp.infoMessages);
            config.items.Clear();
            config.items.AddRange(configTemp.items);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static void saveConfig(ICoreServerAPI api)
        {
            api.StoreModConfig(config, configFile);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static void savePlayerConfig(ICoreServerAPI api)
        {
            api.StoreModConfig(playerConfig, playerconfigFile);
        }
    }
}
