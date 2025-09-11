using System;
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

[assembly: ModInfo("woopEssentials",
    Description = "WoopLands essentials server mod",
    Website = "https://github.com/maus-me/woopEssentials/",
    Authors = new[] { "Th3Dilli", "mausterio" })]

[assembly: InternalsVisibleTo("Tests")]

namespace Th3Essentials;

public delegate void PlayerWithRewardJoin(IServerPlayer player, string discordRewardId);

// ReSharper disable once ClassNeverInstantiated.Global
public class WoopEssentials : ModSystem
{
    internal const string ConfigFile = "woopEssentials.json";

    internal static WoopConfig Config { get; set; } = null!;

    internal static WoopPlayerConfig PlayerConfig { get; private set; } = null!;

    internal static DateTime ShutDownTime;

    internal static readonly string Th3EssentialsModDataKey = "woopEssentials";

    internal ICoreServerAPI Sapi = null!;

    private WoopDiscord? _WoopDiscord;

    private long _restartListener;

    public event PlayerWithRewardJoin? OnPlayerWithRewardJoin;

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI sapi)
    {
        Sapi = sapi;
        try
        {
            Config = Sapi.LoadModConfig<WoopConfig>(ConfigFile);

            if (Config == null)
            {
                Config = new WoopConfig();
                Config.Init();
                Sapi.StoreModConfig(Config, ConfigFile);

                Sapi.Server.LogWarning(Lang.Get("woopessentials:config-init"));
                Sapi.Server.LogWarning(Lang.Get("woopessentials:config-file-info",
                    Path.Combine(GamePaths.ModConfig, ConfigFile)));
            }
        }
        catch (Exception e)
        {
            Sapi.Logger.Error(Lang.Get("woopessentials:th3config-error", e));
            Sapi.Logger.Error(Lang.Get("woopessentials:disabled"));
            return;
        }

        PlayerConfig = new WoopPlayerConfig();

        Sapi.Event.GameWorldSave += GameWorldSave;
        Sapi.Event.PlayerNowPlaying += PlayerNowPlaying;
        Sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, Shutdown);

        if (Config.IsShutdownConfigured())
        {
            LoadRestartTime(DateTime.Now);
            _restartListener = Sapi.Event.RegisterGameTickListener(CheckRestart, 60000);
        }

        // Register entity behaviors early so JSON patches referencing them can load without errors
        Sapi.RegisterEntityBehaviorClass("EntityBehaviorPvp", typeof(EntityBehaviorPvp));

        CommandsLoader.Init(Sapi);
        new Homesystem().Init(Sapi);
        new Starterkitsystem().Init(Sapi);
        new Announcementsystem().Init(Sapi);

        if (Config.IsDiscordConfigured())
        {
            _WoopDiscord = new WoopDiscord();
            _WoopDiscord.Init(this);
        }
        else
        {
            // enable show role here when discord is not active - else it is enabled in the WoopDiscord
            if (Config.ShowRole)
            {
                Sapi.Event.PlayerChat += PlayerChatAsync;
            }

            Sapi.Logger.Debug("Discordbot needs to be configured, functionality disabled!");
        }

        var deathMessages = Sapi.World.Config.GetBool("disableDeathMessages");
        if (Config.IsDiscordConfigured() && !deathMessages)
        {
            Sapi.Event.PlayerDeath += PlayerDeathAsync;
        }

        if (Config.AdminRoles?.Count > 0)
        {
            Sapi.ChatCommands.Create("admins")
                .WithDescription(Lang.Get("woopessentials:slc-admins"))
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(_ => TextCommandResult.Success(WoopUtil.GetAdmins(Sapi)))
                .Validate();
        }

        Sapi.ChatCommands.Create("reloadth3config")
            .WithDescription(Lang.Get("woopessentials:slc-reloadConfig"))
            .RequiresPrivilege(Privilege.controlserver)
            .HandleWith(_ =>
            {
                if (ReloadConfig())
                {
                    LoadRestartTime(DateTime.Now);
                    return TextCommandResult.Success(Lang.Get("woopessentials:cd-reloadconfig-msg"));
                }

                return TextCommandResult.Error(Lang.Get("woopessentials:cd-reloadconfig-fail"));
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
                var restartDate = WoopUtil.GetRestartDate(time, now);
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
            ShutDownTime = WoopUtil.GetRestartDate(Config.ShutdownTime, now);
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
                    ? Lang.Get("woopessentials:restart-in-min")
                    : Lang.Get("woopessentials:restart-in-mins", timeInMinutes);
                SendInGameServerMsg(msg);
                _WoopDiscord?.SendServerMessage(msg);
                Sapi.Logger.Event(msg);
            }
        }

        var totalSeconds = (int)timeTillRestart.TotalSeconds;
        if (!Config.ShutdownEnabled || totalSeconds >= 5) return;

        Sapi.Event.UnregisterGameTickListener(_restartListener);
        if (Config.BackupOnShutdown)
        {
            LockAndKick();
            CreateBackup();
        }

        Sapi.Server.ShutDown();
    }

    private void SendInGameServerMsg(string msg)
    {
        Sapi.SendMessageToGroup(GlobalConstants.GeneralChatGroup,
            !string.IsNullOrEmpty(Config.SystemMsgColor)
                ? $"<font color=\"#{Config.SystemMsgColor}\"><strong>{msg}</strong></font>"
                : msg, EnumChatType.OthersMessage);
    }

    private void CreateBackup()
    {
        var server = (ServerMain)Sapi.World;
            
        var chunkThread = typeof(ServerMain).GetField("chunkThread", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(server) as ChunkServerThread;
        var gameDatabase = typeof(ChunkServerThread).GetField("gameDatabase", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(chunkThread) as GameDatabase;
        var fileInfo = new FileInfo(gameDatabase!.DatabaseFilename);
        var freeDiskSpace = ServerMain.xPlatInterface.GetFreeDiskSpace(fileInfo.DirectoryName);
        if (freeDiskSpace <= fileInfo.Length)
        {
            Sapi.Logger.Warning(
                $"SaveFileSize: {fileInfo.Length / 1000000} MB, FreeDiskSpace: {freeDiskSpace / 1000000} MB");
            Sapi.Logger.Error("Not enought disk space left to create a backup");
            _WoopDiscord?.SendServerMessage(Lang.Get("woopessentials:backup-dc"));
            return;
        }

        var worldName = Path.GetFileNameWithoutExtension(Sapi.WorldManager.CurrentWorldName);
        if (worldName.Length == 0)
        {
            worldName = "world";
        }

        var backupFileName = $"{worldName}-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.vcdbs";

        Sapi.Logger.Event(Lang.Get("woopessentials:backup"));
        _WoopDiscord?.SendServerMessage(Lang.Get("woopessentials:backup-dc"));

        gameDatabase.CreateBackup(backupFileName);
    }

    private void LockAndKick()
    {
        Sapi.Server.Config.Password = new Random().Next().ToString();
        Sapi.Logger.Event($"Temporary server password is: {Sapi.Server.Config.Password}");
        foreach (var player in Sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
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
                var playerData = SerializerUtil.Deserialize<WoopPlayerData>(data);
                PlayerConfig.Add(byPlayer.PlayerUID, playerData);
            }
        }
    }

    private void PlayerDeathAsync(IServerPlayer byPlayer, DamageSource damageSource)
    {
        var msg = WoopUtil.ExtractDeathMessage(byPlayer, damageSource);

        _WoopDiscord?.SendServerMessage(msg);
    }

    private void GameWorldSave()
    {
        if (Config.IsDirty)
        {
            Config.IsDirty = false;
            Sapi.StoreModConfig(Config, ConfigFile);
        }

        PlayerConfig.GameWorldSave(Sapi);
    }

    private void Shutdown()
    {
        GameWorldSave();
    }

    private bool ReloadConfig()
    {
        try
        {
            var configTemp = Sapi.LoadModConfig<WoopConfig>(ConfigFile);
            Config.Reload(configTemp);
        }
        catch (Exception e)
        {
            Sapi.Logger.Error("Error reloading Th3Config: ", e.ToString());
            return false;
        }

        return true;
    }

    public override void Dispose()
    {
        _WoopDiscord?.Dispose();
        Sapi.Event.GameWorldSave -= GameWorldSave;
        Sapi.Event.PlayerNowPlaying -= PlayerNowPlaying;
        Sapi.Event.UnregisterGameTickListener(_restartListener);
        Sapi.Event.PlayerChat -= PlayerChatAsync;
        Sapi.Event.PlayerDeath -= PlayerDeathAsync;
    }

    public static string ToHex(Color c)
    {
        return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
    }
}