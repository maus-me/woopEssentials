using System;
using System.IO;
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
    Website = "https://github.com/Th3Dilli/",
    Authors = new[] { "Th3Dilli" })]
namespace CBSEssentials
{
    public class CBSEssentials : ModSystem
    {
        private const string configFile = "CBSConfig.json";
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
                api.Server.LogWarning("CBSEssentials initialized with default config!!!");
                api.Server.LogWarning("CBSEssentials config file at " + Path.Combine(GamePaths.ModConfig, configFile));
            }

            CommandsLoader.init(api);
            new Homesystem().init(api);
            new Starterkitsystem().init(api);
            new Announcementsystem().init(api);

            api.RegisterCommand("reloadConfig", "realoads CBSConfig", "",
                  (IServerPlayer player, int groupId, CmdArgs args) =>
                  {
                      reloadConfig();
                  }, Privilege.controlserver);
        }

        private void GameWorldSave()
        {
            api.StoreModConfig(config, configFile);
        }

        public void reloadConfig()
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
    }
}
