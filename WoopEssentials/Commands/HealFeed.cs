using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using System.Linq;

namespace WoopEssentials.Commands;

internal class HealFeed : Command
{
    private ICoreServerAPI _sapi = null!;

    internal override void Init(ICoreServerAPI api)
    {

        _sapi = api;
        // /heal - heal player to full health
        api.ChatCommands.Create("heal")
            .WithDescription(Lang.Get("woopessentials:cd-heal"))
            .RequiresPrivilege(Privilege.commandplayer)
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("player"))
            .HandleWith(OnHealCmd)
            .Validate();

        // /feed - set hunger/saturation to full
        api.ChatCommands.Create("feed")
            .WithDescription(Lang.Get("woopessentials:cd-feed"))
            .RequiresPrivilege(Privilege.commandplayer)
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("player"))
            .HandleWith(OnFeedCmd)
            .Validate();

        api.ChatCommands.Create("revive")
            .WithDescription(Lang.Get("woopessentials:cd-revive"))
            .RequiresPrivilege(Privilege.commandplayer)
            .WithArgs(api.ChatCommands.Parsers.OptionalWord("player"))
            .HandleWith(OnHealCmd)
            .Validate();

    }

    private IServerPlayer? ResolveTarget(TextCommandCallingArgs args)
    {
        var playerName = args.Parsers.Count > 0 ? args.Parsers[0].GetValue() as string : null;
        if (!string.IsNullOrEmpty(playerName))
        {
            // Find name case-insensitively for better UX
            var found = _sapi.World.AllOnlinePlayers.FirstOrDefault(p => p.PlayerName.Equals(playerName, System.StringComparison.InvariantCultureIgnoreCase));
            return found as IServerPlayer;
        }
        // No arg supplied: fallback to caller if player
        return args.Caller?.Player as IServerPlayer;
    }

    private TextCommandResult OnFeedCmd(TextCommandCallingArgs args)
    {
        var sp = ResolveTarget(args);
        if (sp == null)
        {
            var raw = args.Parsers.Count > 0 ? args.Parsers[0].GetValue() as string : "";
            return TextCommandResult.Error(Lang.Get("woopessentials:cd-msg-fail", raw ?? ""));
        }

        var ep = sp.Entity;
        if (ep == null) return TextCommandResult.Error("Player not found");

        if (ep is not EntityAgent eagent) return TextCommandResult.Error("Not an agent entity");

        // Use ReceiveSaturation with a high value so it caps at full
        // Parameters: saturation, food category (irrelevant here), saturationLossDelay, nutritionGainMultiplier
        eagent.ReceiveSaturation(10000f, EnumFoodCategory.Unknown, 0f);

        return TextCommandResult.Success(Lang.Get("woopessentials:cd-feed-done"));

    }

    private TextCommandResult OnHealCmd(TextCommandCallingArgs args)
    {
        var sp = ResolveTarget(args);
        if (sp == null)
        {
            var raw = args.Parsers.Count > 0 ? args.Parsers[0].GetValue() as string : "";
            return TextCommandResult.Error(Lang.Get("woopessentials:cd-msg-fail", raw ?? ""));
        }

        var e = sp.Entity;
        if (e == null) return TextCommandResult.Error("Player not found");

        // Revive() fully heals even if dead; if alive, heal via damage.  That was fun to figure out.
        if (e.Alive)
        {
            float healAmount = 9999f;
            var ds = new DamageSource { Source = EnumDamageSource.Internal, Type = EnumDamageType.Heal, SourceEntity = e };
            e.ReceiveDamage(ds, healAmount);
        }
        else
        {
            e.Revive();
        }

        return TextCommandResult.Success(Lang.Get("woopessentials:cd-heal-done"));

    }
}