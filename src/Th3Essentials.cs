﻿using System;
using System.IO;
using Th3Essentials.Announcements;
using Th3Essentials.Commands;
using Th3Essentials.Config;
using Th3Essentials.Discord;
using Th3Essentials.Homepoints;
using Th3Essentials.PlayerData;
using Th3Essentials.Starterkit;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

[assembly: ModInfo("Th3Essentials",
    Description = "Th3Dilli essentials server mod",
    Website = "https://gitlab.com/Th3Dilli/",
    Authors = new[] { "Th3Dilli" })]
namespace Th3Essentials
{
    public class Th3Essentials : ModSystem
    {
        private const string _configFile = "Th3Config.json";

        internal static Th3Config Config { get; private set; }

        internal static Th3PlayerConfig PlayerConfig { get; private set; }

        internal static string Th3EssentialsModDataKey = "Th3Essentials";

        private ICoreServerAPI _api;

        private Th3Discord _th3Discord;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _api = api;
            IServerPlayerData a = _api.PlayerData.GetPlayerDataByUid("");

            try
            {
                Config = _api.LoadModConfig<Th3Config>(_configFile);
                if (Config == null)
                {
                    Config = new Th3Config();
                    Config.Init();
                    _api.StoreModConfig(Config, _configFile);

                    _api.Server.LogWarning(Lang.Get("th3essentials:config-init"));
                    _api.Server.LogWarning(Lang.Get("th3essentials:config-file-info", Path.Combine(GamePaths.ModConfig, _configFile)));
                }
            }
            catch (Exception e)
            {
                _api.Logger.Error(Lang.Get("th3essentials:th3config-error", e));
            }

            PlayerConfig = new Th3PlayerConfig();

            if (Config == null)
            {
                _api.Logger.Error(Lang.Get("th3essentials:disabled"));
                return;
            }

            _api.Event.GameWorldSave += GameWorldSave;
            _api.Event.PlayerNowPlaying += PlayerNowPlaying;

            CommandsLoader.Init(_api);
            new Homesystem().Init(_api);
            new Starterkitsystem().Init(_api);
            new Announcementsystem().Init(_api);
            _th3Discord = new Th3Discord();

            if (Config.IsDiscordConfigured())
            {
                _th3Discord.Init(_api);
            }

            _api.RegisterCommand("reloadconfig", Lang.Get("th3essentials:cd-reloadConfig"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    string response = ReloadConfig() ? Lang.Get("th3essentials:cd-reloadconfig-msg") : Lang.Get("th3essentials:cd-reloadconfig-fail");
                    player.SendMessage(GlobalConstants.GeneralChatGroup, response, EnumChatType.Notification);
                }, Privilege.controlserver);
        }

        private void PlayerNowPlaying(IServerPlayer byPlayer)
        {
            byte[] data = byPlayer.WorldData.GetModdata(Th3EssentialsModDataKey);
            Th3PlayerData playerData;
            if (data != null)
            {
                playerData = SerializerUtil.Deserialize<Th3PlayerData>(data);
            }
            else
            {
                playerData = new Th3PlayerData
                {
                    HomeLimit = Config.HomeLimit,
                    HomeCooldown = Config.HomeCooldown
                };
            }
            playerData.PlayerUID = byPlayer.PlayerUID;
            PlayerConfig.Add(playerData);
        }

        private void GameWorldSave()
        {
            if (Config != null)
            {
                _api.StoreModConfig(Config, _configFile);
            }

            PlayerConfig.GameWorldSave(_api);
        }

        private bool ReloadConfig()
        {
            try
            {
                Th3Config configTemp = _api.LoadModConfig<Th3Config>(_configFile);
                Config.Reload(configTemp);
            }
            catch (Exception e)
            {
                _api.Logger.Error("Error reloading Th3Config: ", e.ToString());
                return false;
            }
            return true;
        }
    }
}
