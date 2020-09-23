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

        internal static CBSConfig Config { get; private set; }

        internal static CBSPlayerConfig PlayerConfig { get; private set; }

        private ICoreServerAPI api;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.api = api;

            api.Event.GameWorldSave += GameWorldSave;

            Config = api.LoadModConfig<CBSConfig>(configFile);

            if (Config == null)
            {
                Config = new CBSConfig();
                Config.Init();
                api.StoreModConfig(Config, configFile);
                api.Server.LogWarning(Lang.Get("cbsessentials:config-init"));
                api.Server.LogWarning(Lang.Get("cbsessentials:config-file-info", Path.Combine(GamePaths.ModConfig, configFile)));
            }

            PlayerConfig = api.LoadModConfig<CBSPlayerConfig>(playerconfigFile);

            if (PlayerConfig == null)
            {
                PlayerConfig = new CBSPlayerConfig();
                api.StoreModConfig(PlayerConfig, playerconfigFile);
                api.Server.LogWarning(Lang.Get("cbsessentials:playerconfig-init"));
                api.Server.LogWarning(Lang.Get("cbsessentials:playerconfig-file-info", Path.Combine(GamePaths.ModConfig, playerconfigFile)));
            }

            api.Server.LogVerboseDebug($"DateTime: {DateTime.MinValue.Equals(new DateTime())}");

            CommandsLoader.Init(api);
            new Homesystem().Init(api);
            new Starterkitsystem().Init(api);
            new Announcementsystem().Init(api);

            api.RegisterCommand("reloadonfig", Lang.Get("cbsessentials:cd-reloadConfig"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    ReloadConfig();
                }, Privilege.controlserver);
        }

        private void GameWorldSave()
        {
            SaveConfig(api);
            SavePlayerConfig(api);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void ReloadConfig()
        {
            CBSConfig configTemp = api.LoadModConfig<CBSConfig>(configFile);
            Config.announcementInterval = configTemp.announcementInterval;
            Config.announcementMessages.Clear();
            Config.announcementMessages.AddRange(configTemp.announcementMessages);
            Config.infoMessages.Clear();
            Config.infoMessages.AddRange(configTemp.infoMessages);
            Config.items.Clear();
            Config.items.AddRange(configTemp.items);
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
