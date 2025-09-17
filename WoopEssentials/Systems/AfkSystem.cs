using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WoopEssentials.Systems;

/// <summary>
/// Minimal AFK tracking system.
/// - Players can toggle AFK via command.
/// - Moving automatically clears AFK.
/// - Inactivity for a period auto-flags AFK.
/// </summary>
internal class AfkSystem : IDisposable
{
    public static AfkSystem Instance { get; private set; } = null!;

    private ICoreServerAPI _sapi = null!;

    private readonly Dictionary<string, AfkState> _states = new();

    private long _tickListenerId;

    // Default AFK timeout (in milliseconds). Kept minimal and internal; can be made configurable later if needed.
    private const int AfkTimeoutMs = 10000;

    // Poll interval for movement/activity checks. Small to be responsive, but not too frequent for performance.
    private const int PollIntervalMs = 3000; // 3 seconds

    private class AfkState
    {
        public bool IsAfk;
        public DateTime LastActiveUtc;
        public Vec3d LastPos = new();
        public float LastYaw;
        public float LastPitch;
    }

    internal void Init(ICoreServerAPI sapi)
    {
        Instance = this;
        _sapi = sapi;

        _sapi.Event.PlayerNowPlaying += OnPlayerNowPlaying;
        _sapi.Event.PlayerLeave += OnPlayerLeave;
        _sapi.Event.PlayerChat += OnPlayerChat;

        // Periodic listener to check for movement and inactivity
        _tickListenerId = _sapi.Event.RegisterGameTickListener(OnTick, PollIntervalMs);
    }

    /// <summary>
    /// Handles player chat events, updating the player's last active time and handling AFK status.
    /// </summary>
    /// <param name="player">The server player who sent the chat message.</param>
    /// <param name="channelId">The ID of the channel where the message was sent.</param>
    /// <param name="message">A reference to the chat message text, which may be modified.</param>
    /// <param name="data">A reference to additional data associated with the message, which may be modified.</param>
    /// <param name="consumed">A reference to a boolean indicating whether the event has been consumed (handled).</param>
    private void OnPlayerChat(IServerPlayer player, int channelId, ref string message, ref string data,
        BoolRef consumed)
    {
        if (!_states.TryGetValue(player.PlayerUID, out var state))
        {
            // Initialize if not present (e.g., server reloaded)
            OnPlayerNowPlaying(player);
            state = _states[player.PlayerUID];
        }

        state.LastActiveUtc = DateTime.UtcNow;

        if (!state.IsAfk) return;

        state.IsAfk = false;
        BroadcastNoLongerAfk(player);
    }

    private void OnPlayerNowPlaying(IServerPlayer player)
    {
        var uid = player.PlayerUID;
        var pos = player.Entity.ServerPos;

        _states[uid] = new AfkState
        {
            IsAfk = false,
            LastActiveUtc = DateTime.UtcNow,
            LastPos = pos.XYZ.Clone(),
            LastYaw = pos.Yaw,
            LastPitch = pos.Pitch
        };
    }

    private void OnPlayerLeave(IServerPlayer player)
    {
        _states.Remove(player.PlayerUID);
    }

    private void OnTick(float dt)
    {
        // Iterate online players to detect movement and inactivity
        var players = _sapi.World.AllOnlinePlayers; // IList<IPlayer>
        if (players == null) return;

        var nowUtc = DateTime.UtcNow;

        foreach (var t in players)
        {
            if (t is not IServerPlayer sp) continue;

            if (!_states.TryGetValue(sp.PlayerUID, out var state))
            {
                // Initialize if not present (e.g., server reloaded)
                OnPlayerNowPlaying(sp);
                state = _states[sp.PlayerUID];
            }

            var epos = sp.Entity.ServerPos;
            if (epos != null)
            {
                // Detect movement using a small epsilon to ignore jitter
                if (Math.Abs(state.LastPos.X - epos.X) > 0.01 ||
                    Math.Abs(state.LastPos.Y - epos.Y) > 0.01 ||
                    Math.Abs(state.LastPos.Z - epos.Z) > 0.01 ||
                    GameMath.AngleRadDistance(state.LastYaw, epos.Yaw) > 0.01f ||
                    GameMath.AngleRadDistance(state.LastPitch, epos.Pitch) > 0.01f
                ) {
                    state.LastPos.Set(epos);
                    state.LastYaw = epos.Yaw;
                    state.LastPitch = epos.Pitch;
                    state.LastActiveUtc = nowUtc;

                    // If player was AFK and moved, clear AFK and announce
                    if (state.IsAfk)
                    {
                        state.IsAfk = false;
                        BroadcastNoLongerAfk(sp);
                    }
                }
            }

            // Auto-AFK after timeout
            if (state.IsAfk) continue;
            var inactiveMs = (nowUtc - state.LastActiveUtc).TotalMilliseconds;
            if (!(inactiveMs >= AfkTimeoutMs)) continue;
            state.IsAfk = true;
            BroadcastNowAfk(sp);
        }
    }

    public void ToggleAfk(IServerPlayer player)
    {
        if (!_states.TryGetValue(player.PlayerUID, out var state))
        {
            OnPlayerNowPlaying(player);
            state = _states[player.PlayerUID];
        }

        state.IsAfk = !state.IsAfk;
        state.LastActiveUtc = DateTime.UtcNow; // prevent instant auto-afk flip

        if (state.IsAfk)
        {
            BroadcastNowAfk(player);
        }
        else
        {
            BroadcastNoLongerAfk(player);
        }
    }

    private void BroadcastNowAfk(IServerPlayer player)
    {
        _sapi.Logger.Audit($"Player {player.PlayerName} is now AFK.");
        _sapi.SendMessageToGroup(0, Lang.Get("woopessentials:afk-now-afk", player.PlayerName), EnumChatType.Notification);
    }

    private void BroadcastNoLongerAfk(IServerPlayer player)
    {
        _sapi.Logger.Audit($"Player {player.PlayerName} is no longer AFK.");
        _sapi.SendMessageToGroup(0, Lang.Get("woopessentials:afk-no-longer-afk", player.PlayerName), EnumChatType.Notification);
    }

    public void Dispose()
    {
        if (_tickListenerId != 0)
        {
            _sapi.Event.UnregisterGameTickListener(_tickListenerId);
            _tickListenerId = 0;
        }
        _sapi.Event.PlayerNowPlaying -= OnPlayerNowPlaying;
        _sapi.Event.PlayerLeave -= OnPlayerLeave;
        _states.Clear();
    }
}
