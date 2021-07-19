using System.Collections.Generic;
using Th3Essentials.Starterkit;

namespace Th3Essentials.Config
{
    public class Th3Config
    {
        public List<string> InfoMessages;

        public List<string> AnnouncementMessages;

        public int AnnouncementInterval;

        public int HomeLimit;

        public int HomeCooldown;

        public List<StarterkitItem> Items;

        public Th3Config()
        {
            HomeLimit = 6;
            HomeCooldown = 5;
            AnnouncementInterval = 10;
            AnnouncementMessages = new List<string>();
            InfoMessages = new List<string>();
            Items = new List<StarterkitItem>();
        }

        public void Init()
        {
            AnnouncementMessages.Add("Welcome to Vintage Story :)");
            AnnouncementMessages.Add("This server is running Th3Essentials.");

            InfoMessages.Add("--------------------");
            InfoMessages.Add("<strong>Important Commands:</strong>");
            InfoMessages.Add(".clients or .online | Shows you all online players");
            InfoMessages.Add("/spawn | Teleport back to the spawn");
            InfoMessages.Add("/sethome [name] | Set a homepoint");
            InfoMessages.Add("/home [name] | Teleport to a homepoint");
            InfoMessages.Add("/starterkit| Recive a one time starterkit");
            InfoMessages.Add("--------------------");
        }

        internal double GetAnnouncementInterval()
        {
            return 1000 * 60 * AnnouncementInterval;
        }
    }
}