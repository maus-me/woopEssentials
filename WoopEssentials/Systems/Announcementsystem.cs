using System;
using System.Timers;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using WoopEssentials.Config;

namespace WoopEssentials.Systems;

internal class Announcementsystem
{
    private ICoreServerAPI _sapi = null!;

    private WoopConfig _config = null!;

    private readonly Random _rng = new Random();
    private int _lastIndex = -1;

    private Timer _announcer = null!;

    public void Init(ICoreServerAPI sapi)
    {
        _sapi = sapi;
        _config = WoopEssentials.Config;

        if (_config.AnnouncementMessages != null && _config.AnnouncementMessages.Count != 0 && _config.AnnouncementInterval > 0)
        {
            _announcer = new Timer(_config.GetAnnouncementInterval());
            _announcer.Elapsed += AnnounceMsg;
            _announcer.AutoReset = true;
            _announcer.Enabled = true;
        }

        // Notify all players when the server is saving the world
        _sapi.Event.GameWorldSave += OnGameWorldSave;
    }

    private void OnGameWorldSave()
    {
        // AnnouncementChatGroupUid defaults to 0 (general chat)
        _sapi.SendMessageToGroup(_config.AnnouncementChatGroupUid,
            _config.AnnouncementLabel + " " + Lang.Get("woopessentials:saving"),
            EnumChatType.Notification);
    }

    private void AnnounceMsg(object? source, ElapsedEventArgs args)
    {
        if (_config.AnnouncementMessages == null || _config.AnnouncementMessages.Count == 0)
        {
            _announcer.Elapsed -= AnnounceMsg;
            return;
        }

        int count = _config.AnnouncementMessages.Count;
        int index;
        if (count == 1)
        {
            index = 0;
        }
        else
        {
            // pick a random index different from previous to avoid immediate repeats when possible
            do
            {
                index = _rng.Next(0, count);
            } while (index == _lastIndex);
        }
        _lastIndex = index;

        // AnnouncementChatGroupId is by default 0 so general chat
        _sapi.SendMessageToGroup(_config.AnnouncementChatGroupUid, $"{_config.AnnouncementLabel} {_config.AnnouncementMessages[index]}", EnumChatType.Notification);
    }
}