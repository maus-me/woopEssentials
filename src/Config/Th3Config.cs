using System;
using System.Collections.Generic;
using System.Text;
using Th3Essentials.Starterkit;

namespace Th3Essentials.Config
{
    public class Th3Config
    {

        public string Token { get; set; }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }

        public string InfoMessage;

        public List<string> AnnouncementMessages;

        public int AnnouncementInterval;

        public int HomeLimit;

        public int HomeCooldown;

        public List<StarterkitItem> Items;

        public bool ShutdownEnabled;

        public TimeSpan ShutdownTime;

        public int[] ShutdownAnnounce;

        public void Init()
        {
            HomeCooldown = 1;
            AnnouncementInterval = 10;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("--------------------");
            sb.AppendLine("<a href=\"https://discord.gg/gE92jaWq9N\">Discord</a>");
            sb.AppendLine("<strong>Important Commands:</strong>");
            sb.AppendLine(".clients or .online | Shows you all online players");
            sb.AppendLine("/spawn | Teleport back to the spawn");
            sb.AppendLine("/back | Teleport back to last position (home/spawn teleport and death)");
            sb.AppendLine("/home | List all homepoints");
            sb.AppendLine("/home [name] | Teleport to a homepoint");
            sb.AppendLine("/sethome [name] | Set a homepoint");
            sb.AppendLine("/delhome [name] | Delete a homepoint");
            sb.AppendLine("/restart [name] | Shows time till next restart");
            sb.AppendLine("/msg [Name] [Message] | Send a message to a player that is online");
            sb.AppendLine("/starterkit | Recive a one time starterkit");
            sb.AppendLine("/serverinfo | Show this information");
            sb.AppendLine("--------------------");
            InfoMessage = sb.ToString();
            ShutdownAnnounce = new int[] { 1, 2, 3, 4, 5, 10, 15, 20, 30 };
        }

        internal double GetAnnouncementInterval()
        {
            return 1000 * 60 * AnnouncementInterval;
        }

        internal bool IsDiscordConfigured()
        {
            return Token != null && ChannelId != 0 && GuildId != 0;
        }

        internal void Reload(Th3Config configTemp)
        {
            AnnouncementInterval = configTemp.AnnouncementInterval;
            if (AnnouncementMessages != null)
            {
                AnnouncementMessages = configTemp.AnnouncementMessages;
            }
            InfoMessage = configTemp.InfoMessage;
            if (Items != null)
            {
                Items = configTemp.Items;
            }
            HomeCooldown = configTemp.HomeCooldown;
            HomeLimit = configTemp.HomeLimit;
            ShutdownEnabled = configTemp.ShutdownEnabled;
            ShutdownAnnounce = configTemp.ShutdownAnnounce;
            ShutdownTime = configTemp.ShutdownTime;
        }
    }
}