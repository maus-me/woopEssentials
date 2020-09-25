using Vintagestory.API.Server;
using Vintagestory.API.Common;
using System.Timers;
using CBSEssentials.Config;

namespace CBSEssentials.Announcements
{
    class Announcementsystem : ModSystem
    {
        private ICoreServerAPI api;

        private CBSConfig config;

        private int currentMsg;

        public Announcementsystem()
        {
            currentMsg = 0;
        }

        public void Init(ICoreServerAPI api)
        {
            this.api = api;
            config = CBSEssentials.Config;

            if (config.announcementMessages.Count != 0)
            {
                Timer announcer = new Timer(config.GetAnnouncementInterval());
                announcer.Elapsed += AnnounceMsg;
                announcer.AutoReset = true;
                announcer.Enabled = true;
            }
        }

        private void AnnounceMsg(object source, ElapsedEventArgs args)
        {
            if (currentMsg >= config.announcementMessages.Count)
            {
                currentMsg = 0;
            }
            api.BroadcastMessageToAllGroups($"<strong>[Info]</strong> {config.announcementMessages[currentMsg]}", EnumChatType.Notification);
            currentMsg++;
        }
    }
}