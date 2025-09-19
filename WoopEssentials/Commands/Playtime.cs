using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace WoopEssentials.Commands;

internal class Playtime : Command
{
    private ICoreServerAPI _sapi = null!;

    internal override void Init(ICoreServerAPI api)
    {
        _sapi = api;

        api.ChatCommands.Create("playtime")
            .WithDescription(Lang.Get("woopessentials:cd-playtime"))
            .RequiresPrivilege(Privilege.chat)
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("player"))
            .HandleWith(OnPlaytime)
            .Validate();
    }

    private TextCommandResult OnPlaytime(TextCommandCallingArgs args)
    {
        var targetName = args.Parsers.Count > 0 ? args.Parsers[0].GetValue() as string : null;
        IServerPlayer? targetPlayer;

        if (!string.IsNullOrWhiteSpace(targetName))
        {
            // Find online player by name (case-insensitive)
            targetPlayer = _sapi.World.AllOnlinePlayers
                .FirstOrDefault(p => p.PlayerName.Equals(targetName, StringComparison.InvariantCultureIgnoreCase)) as IServerPlayer;
            if (targetPlayer == null)
            {
                return TextCommandResult.Error(Lang.Get("woopessentials:playtime-notfound", targetName));
            }
        }
        else
        {
            targetPlayer = args.Caller?.Player as IServerPlayer;
        }

        if (targetPlayer == null)
        {
            return TextCommandResult.Error(Lang.Get("woopessentials:playtime-notfound", targetName ?? ""));
        }

        var uid = targetPlayer.PlayerUID;
        var pdata = WoopEssentials.PlayerConfig.GetPlayerDataByUid(uid);

        // Calculate up-to-date total playtime seconds including current session
        var seconds = pdata.TotalPlaySeconds;
        if (pdata.LastJoinUtc != default)
        {
            var delta = DateTime.UtcNow - pdata.LastJoinUtc;
            if (delta.TotalSeconds > 0) seconds += delta.TotalSeconds;
        }

        var formatted = FormatDuration(TimeSpan.FromSeconds(seconds));

        if (!string.IsNullOrWhiteSpace(targetName) && !targetPlayer.PlayerUID.Equals(args.Caller?.Player?.PlayerUID))
        {
            return TextCommandResult.Success(Lang.Get("woopessentials:playtime-other", targetPlayer.PlayerName, formatted));
        }

        return TextCommandResult.Success(Lang.Get("woopessentials:playtime-self", formatted));
    }

    private static string FormatDuration(TimeSpan ts)
    {
        // Prefer largest units, avoid zero units noise
        if (ts.TotalDays >= 1)
        {
            var days = (int)ts.TotalDays;
            var hours = ts.Hours;
            var minutes = ts.Minutes;
            return $"{days}d {hours}h {minutes}m";
        }
        if (ts.TotalHours >= 1)
        {
            var hours = (int)ts.TotalHours;
            var minutes = ts.Minutes;
            var seconds = ts.Seconds;
            return $"{hours}h {minutes}m {seconds}s";
        }
        if (ts.TotalMinutes >= 1)
        {
            var minutes = (int)ts.TotalMinutes;
            var seconds = ts.Seconds;
            return $"{minutes}m {seconds}s";
        }
        return $"{(int)ts.TotalSeconds}s";
    }
}
