using System.Timers;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Th3Essentials.Announcements
{
    class Announcementsystem
    {
        private ICoreServerAPI _api;

        private Th3Config _config;

        private int _currentMsg;

        public Announcementsystem()
        {
            _currentMsg = 0;
        }

        public void Init(ICoreServerAPI api)
        {
            _api = api;
            _config = Th3Essentials.Config;

            if (_config.AnnouncementMessages.Count != 0)
            {
                Timer announcer = new Timer(_config.GetAnnouncementInterval());
                announcer.Elapsed += AnnounceMsg;
                announcer.AutoReset = true;
                announcer.Enabled = true;
            }
        }

        private void AnnounceMsg(object source, ElapsedEventArgs args)
        {
            if (_currentMsg >= _config.AnnouncementMessages.Count)
            {
                _currentMsg = 0;
            }
            _api.BroadcastMessageToAllGroups($"<strong>[Info]</strong> {_config.AnnouncementMessages[_currentMsg]}", EnumChatType.Notification);
            _currentMsg++;
        }
    }
}