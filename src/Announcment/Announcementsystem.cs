using Vintagestory.API.Server;
using Vintagestory.API.Common;
using System.Timers;
using CBSEssentials.Config;

namespace CBSEssentials.Announcements
{
    class Announcementsystem : ModSystem
    {
        private ICoreServerAPI _api;
        private CBSConfig config;
        private int currentMsg;

        public Announcementsystem()
        {
            currentMsg = 0;
        }

        private const string configFile = "announcements.json";

        public void init(ICoreServerAPI api)
        {
            _api = api;
            this.config = CBSEssentials.config;

            if (config.announcementMessages.Count != 0)
            {
                Timer announcer = new Timer(config.getAnnouncementInterval());
                announcer.Elapsed += announceMsg;
                announcer.AutoReset = true;
                announcer.Enabled = true;
            }
        }

        private void announceMsg(object source, ElapsedEventArgs args) //here is where the magic happens
        {
            if (currentMsg >= config.announcementMessages.Count)
            {
                currentMsg = 0;
            }
            _api.BroadcastMessageToAllGroups($"<strong>[Info]</strong> {config.announcementMessages[currentMsg]}", EnumChatType.Notification);
            currentMsg++;
        }
    }
}