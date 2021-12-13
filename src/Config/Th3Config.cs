using System;
using System.Collections.Generic;
using System.Text;
using Th3Essentials.Starterkit;

namespace Th3Essentials.Config
{
  public class Th3Config
  {
    public string Token = null;

    public ulong ChannelId = 0;

    public ulong GuildId = 0;

    public Th3InfluxConfig InfluxConfig = null;

    public List<ulong> ModerationRoles = null;

    public bool UseEphermalCmdResponse = true;

    public string InfoMessage = null;

    public List<string> AnnouncementMessages = null;

    public int AnnouncementInterval = 0;

    public int HomeLimit = 0;

    public int HomeCooldown = 60;

    public bool SpawnEnabled = false;

    public bool BackEnabled = false;

    public bool MessageEnabled = false;

    public List<StarterkitItem> Items = null;

    public bool ShutdownEnabled = false;

    public TimeSpan ShutdownTime = TimeSpan.Zero; // "00:00:00" in Th3Config.json

    public int[] ShutdownAnnounce = null;

    public string MessageCmdColor = "ff9102";

    public string DiscordChatColor = "7289DA";

    public void Init()
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("--------------------");
      sb.AppendLine("<a href=\"https://discord.gg/\">Discord</a>");
      sb.AppendLine("<strong>Important Commands:</strong>");
      sb.AppendLine(".clients or .online | Shows you all online players");
      sb.AppendLine("/spawn | Teleport back to the spawn");
      sb.AppendLine("/back | Teleport back to last position (home/spawn teleport and death)");
      sb.AppendLine("/home | List all homepoints");
      sb.AppendLine("/home [name] | Teleport to a homepoint");
      sb.AppendLine("/sethome [name] | Set a homepoint");
      sb.AppendLine("/delhome [name] | Delete a homepoint");
      sb.AppendLine("/restart | Shows time till next restart");
      sb.AppendLine("/msg [Name] [Message] | Send a message to a player that is online");
      sb.AppendLine("/starterkit | Recive a one time starterkit");
      sb.AppendLine("/serverinfo | Show this information");
      sb.AppendLine("--------------------");
      InfoMessage = sb.ToString();
    }

    internal double GetAnnouncementInterval()
    {
      return 1000 * 60 * AnnouncementInterval;
    }

    internal bool IsDiscordConfigured()
    {
      return Token != null && Token != string.Empty;
    }

    internal bool IsInlfuxDBConfigured()
    {
      return InfluxConfig != null && InfluxConfig.InlfuxDBURL != string.Empty && InfluxConfig.InlfuxDBToken != string.Empty && InfluxConfig.InlfuxDBBucket != string.Empty && InfluxConfig.InlfuxDBOrg != string.Empty;
    }

    internal void Reload(Th3Config configTemp)
    {
      AnnouncementInterval = configTemp.AnnouncementInterval;
      AnnouncementMessages = configTemp.AnnouncementMessages;
      InfoMessage = configTemp.InfoMessage;
      Items = configTemp.Items;
      HomeCooldown = configTemp.HomeCooldown;
      HomeLimit = configTemp.HomeLimit;
      ShutdownEnabled = configTemp.ShutdownEnabled;
      ShutdownAnnounce = configTemp.ShutdownAnnounce;
      ShutdownTime = configTemp.ShutdownTime;
      SpawnEnabled = configTemp.SpawnEnabled;
      BackEnabled = configTemp.BackEnabled;
      MessageCmdColor = configTemp.MessageCmdColor;
      DiscordChatColor = configTemp.DiscordChatColor;
      MessageEnabled = configTemp.MessageEnabled;
      UseEphermalCmdResponse = configTemp.UseEphermalCmdResponse;
      ModerationRoles = configTemp.ModerationRoles;
      InfluxConfig.InlfuxDBURL = configTemp.InfluxConfig.InlfuxDBURL;
      InfluxConfig.InlfuxDBToken = configTemp.InfluxConfig.InlfuxDBToken;
      InfluxConfig.InlfuxDBBucket = configTemp.InfluxConfig.InlfuxDBBucket;
      InfluxConfig.InlfuxDBOrg = configTemp.InfluxConfig.InlfuxDBOrg;
      InfluxConfig.InlfuxDBOverwriteLogTicks = configTemp.InfluxConfig.InlfuxDBOverwriteLogTicks;
      InfluxConfig.InlfuxDBLogtickThreshold = configTemp.InfluxConfig.InlfuxDBLogtickThreshold;
    }
  }
}