using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;
using WoopEssentials.Config;

namespace WoopEssentials.Systems;

/// <summary>
/// Tracks player playtime and automatically whitelists them after a threshold.
/// Uses WoopPlayerData for persistence across restarts.
/// Intended to ensure that regular players are whitelisted automatically so that during periods of "abuse" where a whitelist may need too temporarily enabled.
/// </summary>
internal class AutoWhitelistSystem
{
    private ICoreServerAPI _sapi = null!;
    private WoopPlayerConfig _playerConfig = null!;

    // 60 minutes
    private const double ThresholdSeconds = 1 * 60;

    private const string Reason = "Auto-whitelist after 60 minutes of playtime";
    private const string ByName = "AutoWhitelist";

    internal void Init(ICoreServerAPI sapi)
    {
        _sapi = sapi;
        _playerConfig = WoopEssentials.PlayerConfig;

        _sapi.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        _sapi.Event.PlayerLeave += OnPlayerLeave;
    }

    private void OnPlayerNowPlaying(IServerPlayer player)
    {
        var pdata = _playerConfig.GetPlayerDataByUid(player.PlayerUID);
        if (pdata.LastJoinUtc == default)
        {
            pdata.LastJoinUtc = DateTime.UtcNow;
            pdata.MarkDirty();
        }
    }

    private void OnPlayerLeave(IServerPlayer player)
    {
        AccumulateAndMaybeWhitelist(player, DateTime.UtcNow);
    }

    private void AccumulateAndMaybeWhitelist(IServerPlayer sp, DateTime nowUtc)
    {
        var pdata = _playerConfig.GetPlayerDataByUid(sp.PlayerUID);
        if (pdata.AutoWhitelisted) return;

        if (pdata.LastJoinUtc != default)
        {
            var delta = nowUtc - pdata.LastJoinUtc;
            if (delta.TotalSeconds > 0)
            {
                pdata.TotalPlaySeconds += delta.TotalSeconds;
                pdata.LastJoinUtc = nowUtc; // continue accumulating next tick
                pdata.MarkDirty();
            }
        }

        if (pdata.TotalPlaySeconds >= ThresholdSeconds)
        {
            TryWhitelist(sp, pdata);
        }

    }

    private void TryWhitelist(IServerPlayer sp, WoopPlayerData pdata)
    {
        try
        {
            var serverMain = (ServerMain)_sapi.World;

            // Try to avoid duplicate whitelist: if server already has this player whitelisted, just set flag
            // We don't have a direct API here; attempting to whitelist again is harmless and will update expiry.


            serverMain.PlayerDataManager.WhitelistPlayer(sp.PlayerName, sp.PlayerUID, ByName, Reason);

            pdata.AutoWhitelisted = true;
            pdata.MarkDirty();

            // Notify player and log
            _sapi.SendMessage(sp, GlobalConstants.GeneralChatGroup,
                Lang.Get("woopessentials:autowhitelist-success", sp.PlayerName), EnumChatType.Notification);
            _sapi.Logger.Audit($"Auto-whitelisted {sp.PlayerName} ({sp.PlayerUID}) after {pdata.TotalPlaySeconds:F0}s playtime.");
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"Failed to auto-whitelist {sp.PlayerName}: {ex}");
        }
    }
}
