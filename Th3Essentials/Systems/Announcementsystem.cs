using System.Timers;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Th3Essentials.Systems;

internal class Announcementsystem
{
    private ICoreServerAPI _sapi = null!;

    private Th3Config _config = null!;

    private int _currentMsg;

    private Timer _announcer = null!;

    public Announcementsystem()
    {
        _currentMsg = 0;
    }

    public void Init(ICoreServerAPI sapi)
    {
        _sapi = sapi;
        _config = Th3Essentials.Config;

        if (_config.AnnouncementMessages != null && _config.AnnouncementMessages.Count != 0 && _config.AnnouncementInterval > 0)
        {
            _announcer = new Timer(_config.GetAnnouncementInterval());
            _announcer.Elapsed += AnnounceMsg;
            _announcer.AutoReset = true;
            _announcer.Enabled = true;
        }
    }

    private void AnnounceMsg(object? source, ElapsedEventArgs args)
    {
        if (_config.AnnouncementMessages == null)
        {
            _announcer.Elapsed -= AnnounceMsg;
            return;
        }
        if (_currentMsg >= _config.AnnouncementMessages.Count)
        {
            _currentMsg = 0;
        }

        // AnnouncementChatGroupId is by default 0 so general chat
        _sapi.SendMessageToGroup(_config.AnnouncementChatGroupUid, $"<strong>[Info]</strong> {_config.AnnouncementMessages[_currentMsg]}", EnumChatType.Notification);
        _currentMsg++;
    }
}