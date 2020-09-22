using System.Collections.Generic;
using CBSEssentials.Starterkit;

namespace CBSEssentials.Config
{
    public class CBSConfig
    {
        public List<string> infoMessages;
        public List<string> announcementMessages;
        public int announcementInterval;
        public List<StarterkitItem> items;

        public CBSConfig()
        {
            announcementInterval = 10;
            announcementMessages = new List<string>();
            infoMessages = new List<string>();
            items = new List<StarterkitItem>();
        }

        public void init()
        {
            announcementMessages.Add("Welcome to Vintage Story :)");
            announcementMessages.Add("This server is running CBSEssentials.");

            infoMessages.Add("--------------------");
            infoMessages.Add("Dieser Server legt den Fokus auf survival.");
            infoMessages.Add("<strong>Wichtige Commands:</strong>");
            infoMessages.Add("/players | Zeigt dir alle Spieler an, die online sind");
            infoMessages.Add("/spawn | Teleportiert dich zu deinem aktuellen Spawn. Um den Spawn zu setzen verwende ein temporal Gear");
            infoMessages.Add("/sethome | Setzt einen Punkt zu dem du mit /home teleportieren kannst.");
            infoMessages.Add("--------------------");
        }

        internal double getAnnouncementInterval()
        {
            return 1000 * 60 * announcementInterval;
        }
    }
}