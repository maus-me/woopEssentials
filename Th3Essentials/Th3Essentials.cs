﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Th3Essentials.Commands;
using Th3Essentials.Config;
using Th3Essentials.Discord;
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

[assembly: InternalsVisibleTo("Tests")]

namespace Th3Essentials;

public delegate void PlayerWithRewardJoin(IServerPlayer player, string discordRewardId);

public class Th3Essentials : ModSystem
{
    internal const string _configFile = "Th3Config.json";

    internal static Th3Config Config { get; set; }

    internal static Th3PlayerConfig PlayerConfig { get; private set; }

    internal static DateTime ShutDownTime;

    internal static string Th3EssentialsModDataKey = "Th3Essentials";

    internal ICoreServerAPI _sapi;

    private Th3Discord _th3Discord;

    private long _restartListener;

    public event PlayerWithRewardJoin OnPlayerWithRewardJoin;

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
                _sapi.Server.LogWarning(Lang.Get("th3essentials:config-file-info",
                    Path.Combine(GamePaths.ModConfig, _configFile)));
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
        _sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);

        if (Config.IsShutdownConfigured())
        {
            LoadRestartTime(DateTime.Now);
            _restartListener = _sapi.Event.RegisterGameTickListener(CheckRestart, 60000);
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

        if (Config.IsDiscordConfigured())
        {
            _sapi.Event.PlayerDeath += PlayerDeathAsync;
        }

        if (Config.AdminRoles?.Count > 0)
        {
            _sapi.ChatCommands.Create("admins")
                .WithDescription(Lang.Get("th3essentials:slc-admins"))
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(_ => TextCommandResult.Success(Th3Util.GetAdmins(_sapi)))
                .Validate();
        }

        _sapi.ChatCommands.Create("reloadth3config")
            .WithDescription(Lang.Get("th3essentials:slc-reloadConfig"))
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(_ =>
            {
                if (ReloadConfig())
                {
                    LoadRestartTime(DateTime.Now);
                    return TextCommandResult.Success(Lang.Get("th3essentials:cd-reloadconfig-msg"));
                }

                return TextCommandResult.Error(Lang.Get("th3essentials:cd-reloadconfig-fail"));
            })
            .Validate();
    }

    internal static void LoadRestartTime(DateTime now)
    {
        if (Config.ShutdownTimes?.Length > 0)
        {
            var next = now;
            var nextSeconds = double.MaxValue;
            foreach (var time in Config.ShutdownTimes)
            {
                var restartDate = Th3Util.GetRestartDate(time, now);
                var timeSpan = restartDate - now;
                if (timeSpan.TotalSeconds < nextSeconds)
                {
                    nextSeconds = timeSpan.TotalSeconds;
                    next = restartDate;
                }
            }

            ShutDownTime = next;
        }
        else
        {
            ShutDownTime = Th3Util.GetRestartDate(Config.ShutdownTime, now);
        }
    }

    internal void PlayerWithRewardJoin(IServerPlayer player, string discordRewardId)
    {
        OnPlayerWithRewardJoin?.Invoke(player, discordRewardId);
    }

    private static void PlayerChatAsync(IServerPlayer byPlayer, int channelId, ref string message, ref string data,
        BoolRef consumed)
    {
        if (Config.ShowRoles == null || Config.ShowRoles.Contains(byPlayer.Role.Code))
        {
            message = string.Format(Config.RoleFormat, ToHex(byPlayer.Role.Color), byPlayer.Role.Name, message);
        }
    }

    private void CheckRestart(float t1)
    {
        var timeTillRestart = ShutDownTime - DateTime.Now;
        var timeInMinutes = (int)timeTillRestart.TotalMinutes;
        if (Config.ShutdownAnnounce != null)
        {
            foreach (var time in Config.ShutdownAnnounce)
            {
                if (time != timeInMinutes) continue;
                var msg = timeInMinutes == 1
                    ? Lang.Get("th3essentials:restart-in-min")
                    : Lang.Get("th3essentials:restart-in-mins", timeInMinutes);
                SendInGameServerMsg(msg);
                _th3Discord?.SendServerMessage(msg);
                _sapi.Logger.Event(msg);
            }
        }

        var totalSeconds = (int)timeTillRestart.TotalSeconds;
        if (!Config.ShutdownEnabled || totalSeconds >= 5) return;

        _sapi.Event.UnregisterGameTickListener(_restartListener);
        if (Config.BackupOnShutdown)
        {
            LockAndKick();
            CreateBackup();
        }

        _sapi.Server.ShutDown();
    }

    private void SendInGameServerMsg(string msg)
    {
        _sapi.SendMessageToGroup(GlobalConstants.GeneralChatGroup,
            !string.IsNullOrEmpty(Config.SystemMsgColor)
                ? $"<font color=\"#{Config.SystemMsgColor}\"><strong>{msg}</strong></font>"
                : msg, EnumChatType.OthersMessage);
    }

    private void CreateBackup()
    {
        var server = (ServerMain)_sapi.World;
            
        var chunkThread = typeof(ServerMain).GetField("chunkThread", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(server) as ChunkServerThread;
        var gameDatabase = typeof(ChunkServerThread).GetField("gameDatabase", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(chunkThread) as GameDatabase;
        var fileInfo = new FileInfo(gameDatabase.DatabaseFilename);
        var freeDiskSpace = ServerMain.xPlatInterface.GetFreeDiskSpace(fileInfo.DirectoryName);
        if (freeDiskSpace <= fileInfo.Length)
        {
            _sapi.Logger.Warning(
                $"SaveFileSize: {fileInfo.Length / 1000000} MB, FreeDiskSpace: {freeDiskSpace / 1000000} MB");
            _sapi.Logger.Error("Not enought disk space left to create a backup");
            return;
        }

        var worldName = Path.GetFileNameWithoutExtension(_sapi.WorldManager.CurrentWorldName);
        if (worldName.Length == 0)
        {
            worldName = "world";
        }

        var backupFileName = $"{worldName}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.vcdbs";

        _sapi.Logger.Event(Lang.Get("th3essentials:backup"));
        _th3Discord?.SendServerMessage(Lang.Get("th3essentials:backup-dc"));

        gameDatabase.CreateBackup(backupFileName);
    }

    private void LockAndKick()
    {
        _sapi.Server.Config.Password = new Random().Next().ToString();
        _sapi.Logger.Event($"Temporary server password is: {_sapi.Server.Config.Password}");
        foreach (var player in _sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
        {
            player.Disconnect("Scheduled Shutdown");
        }
    }

    private void PlayerNowPlaying(IServerPlayer byPlayer)
    {
        if (!PlayerConfig.Players.TryGetValue(byPlayer.PlayerUID, out _))
        {
            var data = byPlayer.WorldData.GetModdata(Th3EssentialsModDataKey);
            if (data != null)
            {
                var playerData = SerializerUtil.Deserialize<Th3PlayerData>(data);
                PlayerConfig.Add(byPlayer.PlayerUID, playerData);
            }
        }
    }

    private void PlayerDeathAsync(IServerPlayer byPlayer, DamageSource damageSource)
    {
        var msg = Th3Util.ExtractDeathMessage(byPlayer, damageSource);

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

    private void Shutdown()
    {
        GameWorldSave();
    }

    private bool ReloadConfig()
    {
        try
        {
            var configTemp = _sapi.LoadModConfig<Th3Config>(_configFile);
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
        _th3Discord?.Dispose();
        _sapi.Event.GameWorldSave -= GameWorldSave;
        _sapi.Event.PlayerNowPlaying -= PlayerNowPlaying;
        _sapi.Event.UnregisterGameTickListener(_restartListener);
        _sapi.Event.PlayerChat -= PlayerChatAsync;
        _sapi.Event.PlayerDeath -= PlayerDeathAsync;
    }

    public static string ToHex(Color c)
    {
        return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
    }
}