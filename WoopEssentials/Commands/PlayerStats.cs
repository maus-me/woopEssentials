using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace WoopEssentials.Commands;

internal class PlayerStats : Command
{
    private ICoreServerAPI _sapi = null!;

    internal override void Init(ICoreServerAPI api)
    {
        _sapi = api;
        api.ChatCommands.Create("seen")
            .WithDescription(Lang.Get("woopessentials:cd-seen"))
            .RequiresPrivilege(Privilege.chat)
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("player"))
            .HandleWith(OnSeen)
            .Validate();
    }


    private TextCommandResult OnSeen(TextCommandCallingArgs args)
    {
        var playerName = args.Parsers.Count > 0 ? args.Parsers[0].GetValue() as string : null;
        
        if (string.IsNullOrEmpty(playerName))
        {
            // If no player name is specified, default to the caller
            if (args.Caller?.Player != null)
            {
                playerName = args.Caller.Player.PlayerName;
            }
            else
            {
                // This might happen if called from console with no args
                return TextCommandResult.Error("Please specify a player name");
            }
        }

        // First check if the player is currently online
        var onlinePlayer = _sapi.World.AllOnlinePlayers.FirstOrDefault(
            p => p.PlayerName.Equals(playerName, StringComparison.InvariantCultureIgnoreCase));
        
        if (onlinePlayer != null)
        {
            // Player is currently online
            return TextCommandResult.Success(Lang.Get("woopessentials:seen-online-now", onlinePlayer.PlayerName));
        }

        // Player is not online, lookup their data in playerdata.json
        var playerData = _sapi.PlayerData.GetPlayerDataByLastKnownName(playerName);

        if (playerData == null)
        {
            // Player not found in data
            return TextCommandResult.Error(Lang.Get("woopessentials:seen-notfound", playerName));
        }

        // Return the player date information
        var lastJoinDateTime = DateTime.Parse(playerData.LastJoinDate).ToLocalTime();
        var timeSinceLastJoin = DateTime.Now - lastJoinDateTime;
        var timeSinceText = WoopUtil.PrettyTime(timeSinceLastJoin);
        var lastseen = $"{lastJoinDateTime.ToString("g")} ({timeSinceText} ago)";
        return TextCommandResult.Success(Lang.Get("woopessentials:seen-lastlogout", playerData.LastKnownPlayername, lastseen));
    }
}
