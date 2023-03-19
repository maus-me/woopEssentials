using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Th3Essentials.Config
{
    public class Th3Config
    {
        [JsonIgnore]
        public bool IsDirty;

        public Th3DiscordConfig DiscordConfig = null;

        public Th3InfluxConfig InfluxConfig = null;

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

        public bool BackupOnShutdown = false;

        public TimeSpan ShutdownTime = TimeSpan.Zero; // "00:00:00" in Th3Config.json
        public TimeSpan[] ShutdownTimes = null;

        public int[] ShutdownAnnounce = null;

        public string MessageCmdColor = "ff9102";

        public bool ShowRole = false;

        public string RoleFormat = "<font size=\"18\" color=\"{0}\"><strong>[{1}]</strong></font>{2}";

        public List<string> AdminRoles = null;

        public bool WarpEnabled = false;

        public List<HomePoint> WarpLocations = null;

        public void Init()
        {
            StringBuilder sb = new StringBuilder();
            _ = sb.AppendLine("--------------------");
            _ = sb.AppendLine("<a href=\"https://discord.gg/\">Discord</a>");
            _ = sb.AppendLine("<strong>Important Commands:</strong>");
            _ = sb.AppendLine(".clients or .online | Shows you all online players");
            _ = sb.AppendLine("/spawn | Teleport back to the spawn");
            _ = sb.AppendLine("/back | Teleport back to last position (home/spawn teleport and death)");
            _ = sb.AppendLine("/home | List all homepoints");
            _ = sb.AppendLine("/home [name] | Teleport to a homepoint");
            _ = sb.AppendLine("/sethome [name] | Set a homepoint");
            _ = sb.AppendLine("/delhome [name] | Delete a homepoint");
            _ = sb.AppendLine("/restart | Shows time till next restart");
            _ = sb.AppendLine("/msg [Name] [Message] | Send a message to a player that is online");
            _ = sb.AppendLine("/starterkit | Recive a one time starterkit");
            _ = sb.AppendLine("/serverinfo | Show this information");
            _ = sb.AppendLine("--------------------");
            InfoMessage = sb.ToString();

            DiscordConfig = new Th3DiscordConfig();
            InfluxConfig = new Th3InfluxConfig();
        }

        internal double GetAnnouncementInterval()
        {
            return 1000 * 60 * AnnouncementInterval;
        }

        internal bool IsDiscordConfigured()
        {
            return DiscordConfig != null &&
                    DiscordConfig.Token?.Length > 0;
        }

        internal bool IsInlfuxDBConfigured()
        {
            return InfluxConfig != null &&
                    InfluxConfig.InlfuxDBURL?.Length > 0 &&
                    InfluxConfig.InlfuxDBToken?.Length > 0 &&
                    InfluxConfig.InlfuxDBBucket?.Length > 0 &&
                    InfluxConfig.InlfuxDBOrg?.Length > 0;
        }

        internal bool IsShutdownConfigured()
        {
            return ShutdownAnnounce?.Length > 0 || ShutdownEnabled || ShutdownTimes?.Length > 0;
        }

        public HomePoint FindWarpByName(string name)
        {
            return WarpLocations?.Find(point => point.Name == name);
        }

        public void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }

        internal void Reload(Th3Config configTemp)
        {
            AnnouncementInterval = configTemp.AnnouncementInterval;
            AnnouncementMessages = configTemp.AnnouncementMessages;
            InfoMessage = configTemp.InfoMessage;
            Items = configTemp.Items;

            HomeCooldown = configTemp.HomeCooldown;
            HomeLimit = configTemp.HomeLimit;

            SpawnEnabled = configTemp.SpawnEnabled;
            BackEnabled = configTemp.BackEnabled;

            ShutdownEnabled = configTemp.ShutdownEnabled;
            ShutdownAnnounce = configTemp.ShutdownAnnounce;
            ShutdownTime = configTemp.ShutdownTime;
            ShutdownTimes = configTemp.ShutdownTimes;

            MessageCmdColor = configTemp.MessageCmdColor;
            MessageEnabled = configTemp.MessageEnabled;

            ShowRole = configTemp.ShowRole;
            RoleFormat = configTemp.RoleFormat;
            AdminRoles = configTemp.AdminRoles;

            WarpEnabled = configTemp.WarpEnabled;
            WarpLocations = configTemp.WarpLocations;

            if (configTemp.DiscordConfig != null)
            {
                if (DiscordConfig == null)
                {
                    DiscordConfig = new Th3DiscordConfig();
                }

                DiscordConfig.DiscordChatColor = configTemp.DiscordConfig.DiscordChatColor;
                DiscordConfig.UseEphermalCmdResponse = configTemp.DiscordConfig.UseEphermalCmdResponse;
                DiscordConfig.Token = configTemp.DiscordConfig.Token;
                DiscordConfig.ChannelId = configTemp.DiscordConfig.ChannelId;
                DiscordConfig.GuildId = configTemp.DiscordConfig.GuildId;
                DiscordConfig.ModerationRoles = configTemp.DiscordConfig.ModerationRoles;
                DiscordConfig.LinkedAccounts = configTemp.DiscordConfig.LinkedAccounts;
                DiscordConfig.RoleRewardsFormat = configTemp.DiscordConfig.RoleRewardsFormat;
                DiscordConfig.RewardIdToName = configTemp.DiscordConfig.RewardIdToName;
                DiscordConfig.Rewards = configTemp.DiscordConfig.Rewards;
            }

            if (configTemp.InfluxConfig != null)
            {
                if (InfluxConfig == null)
                {
                    InfluxConfig = new Th3InfluxConfig();
                }

                InfluxConfig.InlfuxDBURL = configTemp.InfluxConfig.InlfuxDBURL;
                InfluxConfig.InlfuxDBToken = configTemp.InfluxConfig.InlfuxDBToken;
                InfluxConfig.InlfuxDBBucket = configTemp.InfluxConfig.InlfuxDBBucket;
                InfluxConfig.InlfuxDBOrg = configTemp.InfluxConfig.InlfuxDBOrg;
                InfluxConfig.InlfuxDBOverwriteLogTicks = configTemp.InfluxConfig.InlfuxDBOverwriteLogTicks;
                InfluxConfig.DataCollectInterval = configTemp.InfluxConfig.DataCollectInterval;
            }
        }
    }
}