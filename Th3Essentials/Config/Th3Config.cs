using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Th3Essentials.Config
{
    public class Th3Config
    {
        public bool IsDirty;

        public Th3DiscordConfig DiscordConfig;

        public Th3InfluxConfig InfluxConfig;

        public string InfoMessage;

        public List<string> AnnouncementMessages;

        public int AnnouncementInterval;

        public int HomeLimit = -1;

        public int HomeCooldown = 60;
        public int BackCooldown = 60;
        public bool ExcludeHomeFromBack = false;
        public StarterkitItem? HomeItem;
        public StarterkitItem? SetHomeItem;

        public bool SpawnEnabled;

        public bool BackEnabled;

        public bool MessageEnabled;

        public List<StarterkitItem> Items;

        public bool ShutdownEnabled;

        public bool BackupOnShutdown = false;

        public TimeSpan ShutdownTime = TimeSpan.Zero; // "00:00:00" in Th3Config.json
        public TimeSpan[] ShutdownTimes;

        public int[] ShutdownAnnounce;

        public string MessageCmdColor = "ff9102";
        
        public string SystemMsgColor = "ff9102";

        public bool ShowRole;
        public List<string>? ShowRoles;

        public string RoleFormat = "<font size=\"18\" color=\"{0}\"><strong>[{1}]</strong></font>{2}";

        public List<string> AdminRoles;

        public bool WarpEnabled;

        public List<HomePoint> WarpLocations;

        public string ChatTimestampFormat;

        public bool EnableSmite;
        
        public int RandomTeleportRadius = 0;
        
        public int RandomTeleportCooldown = 60;

        public StarterkitItem? RandomTeleportItem;
        
        public int TeleportToPlayerCooldown = 60;
        public bool TeleportToPlayerEnabled = false;
        public StarterkitItem? TeleportToPlayerItem;

        public Dictionary<string, RoleConfig>? RoleConfig;

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
            return ShutdownAnnounce?.Length > 0 || ShutdownEnabled;
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
            SetHomeItem = configTemp.SetHomeItem;
            HomeItem = configTemp.HomeItem;
            BackCooldown = configTemp.BackCooldown;
            ExcludeHomeFromBack = configTemp.ExcludeHomeFromBack;

            SpawnEnabled = configTemp.SpawnEnabled;
            BackEnabled = configTemp.BackEnabled;

            ShutdownEnabled = configTemp.ShutdownEnabled;
            ShutdownAnnounce = configTemp.ShutdownAnnounce;
            ShutdownTime = configTemp.ShutdownTime;
            ShutdownTimes = configTemp.ShutdownTimes;

            MessageCmdColor = configTemp.MessageCmdColor;
            MessageEnabled = configTemp.MessageEnabled;
            
            SystemMsgColor = configTemp.SystemMsgColor;

            ShowRole = configTemp.ShowRole;
            ShowRoles = configTemp.ShowRoles;
            RoleFormat = configTemp.RoleFormat;
            AdminRoles = configTemp.AdminRoles;

            WarpEnabled = configTemp.WarpEnabled;
            WarpLocations = configTemp.WarpLocations;
            ChatTimestampFormat = configTemp.ChatTimestampFormat;
            EnableSmite = configTemp.EnableSmite;
            
            RandomTeleportRadius = configTemp.RandomTeleportRadius;
            RandomTeleportCooldown = configTemp.RandomTeleportCooldown;
            RandomTeleportItem = configTemp.RandomTeleportItem;
            
            TeleportToPlayerCooldown = configTemp.TeleportToPlayerCooldown;
            TeleportToPlayerEnabled = configTemp.TeleportToPlayerEnabled;
            TeleportToPlayerItem = configTemp.TeleportToPlayerItem;

            RoleConfig = configTemp.RoleConfig;

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
                DiscordConfig.DiscordChatRelay = configTemp.DiscordConfig.DiscordChatRelay;
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