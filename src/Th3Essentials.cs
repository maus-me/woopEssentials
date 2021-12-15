using System;
using System.IO;
using Th3Essentials.Announcements;
using Th3Essentials.Commands;
using Th3Essentials.Config;
using Th3Essentials.Discordbot;
using Th3Essentials.Homepoints;
using Th3Essentials.Influxdb;
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

    private Th3Influxdb _th3Influx;

    private readonly string[] TemporalStorm;

    public Th3Essentials()
    {
      TemporalStorm = new string[7]{
                Lang.Get("A light temporal storm is approaching"),
                Lang.Get("A light temporal storm is imminent"),
                Lang.Get("A medium temporal storm is approaching"),
                Lang.Get("A medium temporal storm is imminent"),
                Lang.Get("A heavy temporal storm is approaching"),
                Lang.Get("A heavy temporal storm is imminent"),
                Lang.Get("The temporal storm seems to be waning")
            };
    }

    public override bool ShouldLoad(EnumAppSide forSide)
    {
      return forSide == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
      _api = api;
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
        _api.Logger.Error(Lang.Get("th3essentials:disabled"));
        return;
      }

      PlayerConfig = new Th3PlayerConfig();

      _api.Event.GameWorldSave += GameWorldSave;
      _api.Event.PlayerNowPlaying += PlayerNowPlaying;
      _api.Logger.EntryAdded += LogEntryAdded;

      if ((Config.ShutdownAnnounce != null && Config.ShutdownAnnounce.Length > 0) || (Config.ShutdownTime != null && Config.ShutdownEnabled))
      {
        _api.Event.RegisterGameTickListener(CheckRestart, 60000);
      }

      CommandsLoader.Init(_api);
      new Homesystem().Init(_api);
      new Starterkitsystem().Init(_api);
      new Announcementsystem().Init(_api);
      _th3Discord = new Th3Discord();
      _th3Influx = new Th3Influxdb();

      if (Config.IsDiscordConfigured())
      {
        _th3Discord.Init(_api);
      }
      else
      {
        _api.Logger.Debug("Discordbot needs to be configured, functionality disabled!!!");
      }

      if (Config.IsInlfuxDBConfigured())
      {
        _api.Logger.Debug("InlfuxDB is ready");
        _th3Influx.Init(_api);
      }

      if (Config.IsInlfuxDBConfigured() || Config.IsDiscordConfigured())
      {
        _api.Event.PlayerDeath += PlayerDeathAsync;
      }

      _api.RegisterCommand("reloadth3config", Lang.Get("th3essentials:cd-reloadConfig"), string.Empty,
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

    private void CheckRestart(float t1)
    {
      int TimeInMinutes = (int)Th3Util.GetTimeTillRestart().TotalMinutes;
      if (Config.ShutdownAnnounce != null)
      {
        foreach (int time in Config.ShutdownAnnounce)
        {
          if (time == TimeInMinutes)
          {
            string msg = TimeInMinutes == 1 ? Lang.Get("th3essentials:restart-in-min") : Lang.Get("th3essentials:restart-in-mins", TimeInMinutes);
            _api.SendMessageToGroup(GlobalConstants.GeneralChatGroup, msg, EnumChatType.OthersMessage);
            _th3Discord.SendServerMessage(msg);
            _api.Logger.Debug(msg);
          }
        }
      }
      if (Config.ShutdownEnabled && TimeInMinutes < 1)
      {
        _api.Server.ShutDown();
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

    private void LogEntryAdded(EnumLogType logType, string message, object[] args)
    {
      if (logType == EnumLogType.Notification && args?.Length > 0 && args[0] is string msg)
      {
        foreach (string tmpMsg in TemporalStorm)
        {
          if (msg.Equals(tmpMsg))
          {
            _th3Discord.SendServerMessage(Lang.Get("th3essentials:temporalStormPrefix") + msg);
            return;
          }
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
          Th3Influxdb.Instance.PlayerDied(byPlayer, key);
        }
        else
        {
          msg = Lang.Get("th3essentials:playerdeath", byPlayer.PlayerName);
          Th3Influxdb.Instance.PlayerDied(byPlayer, "unknown");
        }
      }
      else
      {
        msg = Lang.Get("th3essentials:playerdeath", byPlayer.PlayerName);
        Th3Influxdb.Instance.PlayerDied(byPlayer, "unknown");
      }
      _th3Discord.SendServerMessage(msg);
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
