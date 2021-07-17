using Vintagestory.API.Server;
using Vintagestory.API.Common;
using System.Timers;
using Th3Essentials.Config;

namespace Th3Essentials.Announcements
{
    class Announcementsystem : ModSystem
    {
        private ICoreServerAPI api;

        private Th3Config config;

        private int currentMsg;

        public Announcementsystem()
        {
            currentMsg = 0;
        }

        public void Init(ICoreServerAPI api)
        {
            this.api = api;
            config = Th3Essentials.config;

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