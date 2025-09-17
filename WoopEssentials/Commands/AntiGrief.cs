using System;
using System.Globalization;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;
using WoopEssentials.Systems;

namespace WoopEssentials.Commands;

internal class AntiGrief : Command
{
    private ICoreServerAPI _sapi = null!;

    private int _mapSizeX;
    private int _mapSizeZ;

    internal override void Init(ICoreServerAPI api)
    {
        _sapi = api;

        // Cache world size for coordinate offsetting
        _mapSizeX = _sapi.WorldManager.MapSizeX / 2;
        _mapSizeZ = _sapi.WorldManager.MapSizeZ / 2;

        api.ChatCommands.Create("wp")
            .WithDescription(Lang.Get("woopessentials:cd-wp"))
            .RequiresPrivilege(Privilege.controlserver)
            .RequiresPlayer()
            .BeginSubCommand("here")
                .HandleWith(OnHere)
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription(Lang.Get("woopessentials:cd-wp-here"))
                .RequiresPlayer()
            .EndSubCommand()
            .BeginSubCommand("chunk")
                .HandleWith(OnHere)
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription(Lang.Get("woopessentials:cd-wp-chunk"))
                .RequiresPlayer()
            .EndSubCommand()
            .BeginSubCommand("radius")
                .HandleWith(OnHere)
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription(Lang.Get("woopessentials:cd-wp-radius"))
                .RequiresPlayer()
            .EndSubCommand()
            .HandleWith(OnHere)
            .Validate();
    }

    /// <summary>
    /// Handles the "/wp chunk" command to display block change history within a specified chunk of the player.
    /// </summary>
    /// <param name="args">The arguments passed with the command.</param>
    /// <returns>A TextCommandResult indicating the success or failure of the operation and the relevant message.</returns>
    private TextCommandResult OnChunk(TextCommandCallingArgs args)
    {
        if (args.Caller.Player is not IServerPlayer player)
        {
            return TextCommandResult.Success("This command can only be used by a player.");
        }

        return null;
    }


    /// <summary>
    /// Handles the "/wp radius" command to display block change history within a specified radius of the player.
    /// </summary>
    /// <param name="args">The arguments passed with the command.</param>
    /// <returns>A TextCommandResult indicating the success or failure of the operation and the relevant message.</returns>
    private TextCommandResult OnRadius(TextCommandCallingArgs args)
    {
        if (args.Caller.Player is not IServerPlayer player)
        {
            return TextCommandResult.Success("This command can only be used by a player.");
        }

        return null;
    }


    /// <summary>
    /// Handles the "/wp here" command to display the history of block changes at the player's current block selection.
    /// </summary>
    /// <param name="args">The arguments passed with the command.</param>
    /// <returns>A TextCommandResult indicating the success or failure of the operation and the relevant message.</returns>
    private TextCommandResult OnHere(TextCommandCallingArgs args)
    {
        if (args.Caller.Player is not IServerPlayer player)
        {
            return TextCommandResult.Success("This command can only be used by a player.");
        }

        var sel = player.CurrentBlockSelection;
        if (sel == null)
        {
            return TextCommandResult.Success("Look at a block and run /wp to see its recent history.");
        }

        const int limit = 5;

        var pos = sel.Position;
        var events = AntiGriefsystem.Instance.GetHistoryAt(pos, limit);

        // We want to show the player the actual in-game coordinates and not the serverside coordinates
        pos.X -= _mapSizeX;
        pos.Z -= _mapSizeZ;

        if (events.Length == 0)
        {
            return TextCommandResult.Success($"No history for {pos.X},{pos.Y},{pos.Z}.");
        }

        var sb = new StringBuilder();
        sb.AppendLine($"History at {pos.X},{pos.Y},{pos.Z} (latest {events.Length}):");
        var ci = CultureInfo.InvariantCulture;
        foreach (var ev in events)
        {
            var span = DateTimeOffset.Now - DateTimeOffset.FromUnixTimeMilliseconds(ev.TsMs).ToLocalTime();
            var dt = WoopUtil.PrettyTime(span) + " ago";
            var action = ev.Action switch
            {
                0 => "broke",
                1 => "placed",
                _ => ev.Action.ToString(ci)
            };
            var who = ResolvePlayerName(ev.PlayerUid);
            var detail = ev.Action switch
            {
                0 or 1 => ResolveBlockName(ev.BlockId),
                _ => throw new ArgumentOutOfRangeException()
            };
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "<font color=\"#808080\">{0}</font><font color=\"#808080\"> - </font><font color=\"#1E90FF\">{1} </font><font color=\"#FFFFFF\">{2} </font> <font color=\"#1E90FF\">{3} </font>", dt, who, action, detail));
        }

        return TextCommandResult.Success(sb.ToString());
    }

    private string ResolveBlockName(int? blockId)
    {
        try
        {
            if (blockId == null) return "unknown";

            var block = _sapi.World.BlockAccessor.GetBlock(blockId.Value);
            // If the id is invalid, BlockAccessor usually returns the air block (id 0) or null
            if (block == null)
            {
                return blockId.Value.ToString(CultureInfo.InvariantCulture);
            }

            // Prefer the asset code (e.g. game:stone-granite), it's fast and unambiguous
            var code = block.Code?.ToString();
            if (!string.IsNullOrEmpty(code)) return code!;

            // Fallback to numeric id as last resort
            return blockId.Value.ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            return blockId?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
        }
    }

    /// <summary>
    /// Resolves the player's name from a given player UID.
    /// </summary>
    /// <param name="uid">The player's unique identifier.</param>
    /// <returns>The player's name if found, otherwise "unknown".</returns>
    private string ResolvePlayerName(string? uid)
    {
        if (string.IsNullOrEmpty(uid)) return "unknown";

        // Prefer live player name if online
        var online = _sapi.World.PlayerByUid(uid);
        if (online != null && !string.IsNullOrEmpty(online.PlayerName))
        {
            return online.PlayerName;
        }

        // Fallback to last known name from saved player data
        try
        {
            if (_sapi.PlayerData is PlayerDataManager pdm && pdm.PlayerDataByUid.TryGetValue(uid, out var pdata))
            {
                if (!string.IsNullOrEmpty(pdata.LastKnownPlayername)) return pdata.LastKnownPlayername;
            }
        }
        catch { /* ignore */ }

        return uid;
    }
}