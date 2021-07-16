using System.Collections.Generic;
using Th3Essentials.Starterkit;

namespace Th3Essentials.Config
{
    public class Th3Config
    {
        public List<string> infoMessages;

        public List<string> announcementMessages;

        public int announcementInterval;

        public List<StarterkitItem> items;

        public Th3Config()
        {
            announcementInterval = 10;
            announcementMessages = new List<string>();
            infoMessages = new List<string>();
            items = new List<StarterkitItem>();
        }

        public void Init()
        {
            announcementMessages.Add("Welcome to Vintage Story :)");
            announcementMessages.Add("This server is running Th3Essentials.");

            infoMessages.Add("--------------------");
            infoMessages.Add("Dieser Server legt den Fokus auf survival.");
            infoMessages.Add("<strong>Wichtige Commands:</strong>");
            infoMessages.Add(".clients | Zeigt dir alle Spieler an, die online sind");
            infoMessages.Add("/spawn | Teleportiert dich zum Welt Spawn.");
            infoMessages.Add("/sethome [name] | Setzt einen Homepunkt.");
            infoMessages.Add("/home [name] | Teleportiert dich zu einem Homepunkt.");
            infoMessages.Add("--------------------");
        }

        internal double GetAnnouncementInterval()
        {
            return 1000 * 60 * announcementInterval;
        }
    }
}