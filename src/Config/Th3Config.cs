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

        public List<string> InfoMessages;

        public List<string> AnnouncementMessages;

        public int AnnouncementInterval;

        public int HomeLimit;

        public int HomeCooldown;

        public List<StarterkitItem> Items;

        public bool ShutdownEnabled;

        public TimeSpan ShutdownTime;

        public int[] ShutdownAnnounce;

        public Th3Config()
        {
            Token = "";
            ChannelId = 0;
            GuildId = 0;
            HomeLimit = 6;
            HomeCooldown = 5;
            AnnouncementInterval = 10;
            AnnouncementMessages = new List<string>();
            InfoMessages = new List<string>();
            Items = new List<StarterkitItem>();
            ShutdownEnabled = true;
            ShutdownTime = new TimeSpan();
            ShutdownAnnounce = new int[0];
        }

        public void Init()
        {
            InfoMessages.Add("--------------------");
            InfoMessages.Add("<strong>Important Commands:</strong>");
            InfoMessages.Add(".clients or .online | Shows you all online players");
            InfoMessages.Add("/spawn | Teleport back to the spawn");
            InfoMessages.Add("/sethome [name] | Set a homepoint");
            InfoMessages.Add("/home [name] | Teleport to a homepoint");
            InfoMessages.Add("/starterkit| Recive a one time starterkit");
            InfoMessages.Add("--------------------");
            ShutdownAnnounce = new int[] { 1, 2, 3, 4, 5, 10, 15, 20, 30 };
        }

        internal double GetAnnouncementInterval()
        {
            return 1000 * 60 * AnnouncementInterval;
        }

        internal bool IsDiscordConfigured()
        {
            return Token != "" && ChannelId != 0 && GuildId != 0;
        }
    }
}