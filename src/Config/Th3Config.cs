using System;
using System.Collections.Generic;
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
            InfoMessage = "--------------------\n" +
            "<strong>Important Commands:</strong>\n" +
            ".clients or .online | Shows you all online players\n" +
            "/spawn | Teleport back to the spawn\n" +
            "/sethome [name] | Set a homepoint\n" +
            "/home [name] | Teleport to a homepoint\n" +
            "/starterkit| Recive a one time starterkit\n" +
            "--------------------";
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
                AnnouncementMessages.Clear();
                AnnouncementMessages = configTemp.AnnouncementMessages;
            }
            InfoMessage = configTemp.InfoMessage;
            if (Items != null)
            {
                Items.Clear();
                Items.AddRange(configTemp.Items);
            }
            HomeCooldown = configTemp.HomeCooldown;
            HomeLimit = configTemp.HomeLimit;
            ShutdownEnabled = configTemp.ShutdownEnabled;
            ShutdownAnnounce = configTemp.ShutdownAnnounce;
            ShutdownTime = configTemp.ShutdownTime;
        }
    }
}