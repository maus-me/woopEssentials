using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using System;
using Th3Essentials.Systems;

namespace Th3Essentials.Commands;

internal class PvP : Command
{
    internal override void Init(ICoreServerAPI api)
    {
        if (!Th3Essentials.Config.EnablePvPToggle) return;

        api.ChatCommands.Create("pvp")
            .WithDescription(Lang.Get("th3essentials:cd-pvp"))
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .HandleWith(OnPvP)
            .BeginSubCommand("on")
                .WithDescription(Lang.Get("th3essentials:cd-pvp-toggle-on"))
                .IgnoreAdditionalArgs()
                .WithAlias("enable")
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(OnPvPToggleOn)
            .EndSubCommand()
            .BeginSubCommand("off")
                .WithDescription(Lang.Get("th3essentials:cd-pvp-toggle-off"))
                .IgnoreAdditionalArgs()
                .WithAlias("disable")
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(OnPvPToggleOff)
            .EndSubCommand()
            ;
    }

    private TextCommandResult OnPvP(TextCommandCallingArgs args)
    {
        // Show the player their current PvP status
        var pvp = args.Caller.Player.Entity.GetBehavior<EntityBehaviorPvp>();
        if (pvp == null)
        {
            return TextCommandResult.Error("No PVP Behavior Set.");
        }

        if (pvp.Enabled)
        {
            // Also show cooldown remaining if any
            if (pvp.IsCooldownActive(out var remaining))
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:pvp-status-enabled") +
                    $" (cooldown: {Math.Ceiling(remaining.TotalSeconds)}s)");
            }
            return TextCommandResult.Success(Lang.Get("th3essentials:pvp-status-enabled"));
        }
        else
        {
            return TextCommandResult.Success(Lang.Get("th3essentials:pvp-status-disabled"));
        }
    }

    private TextCommandResult OnPvPToggleOn(TextCommandCallingArgs args)
    {
        var pvp = args.Caller.Player.Entity.GetBehavior<EntityBehaviorPvp>();
        if (pvp == null)
        {
            return TextCommandResult.Error("No PVP Behavior Set.");
        }
        if (pvp.Enabled)
        {
            return TextCommandResult.Success(Lang.Get("th3essentials:pvp-already-enabled"));
        }

        pvp.Enabled = true;
        pvp.StartCooldown(EntityBehaviorPvp.DefaultCooldownSeconds);

        return TextCommandResult.Success(Lang.Get("th3essentials:pvp-now-enabled"));
    }

    private TextCommandResult OnPvPToggleOff(TextCommandCallingArgs args)
    {
        var pvp = args.Caller.Player.Entity.GetBehavior<EntityBehaviorPvp>();
        if (pvp == null)
        {
            return TextCommandResult.Error("No PVP Behavior Set.");
        }
        if (!pvp.Enabled)
        {
            return TextCommandResult.Success(Lang.Get("th3essentials:pvp-already-disabled"));
        }

        // Enforce cooldown after enabling or recent combat
        if (pvp.IsCooldownActive(out var remaining))
        {
            var secs = Math.Ceiling(remaining.TotalSeconds);
            return TextCommandResult.Error($"You cannot disable PvP yet. Cooldown: {secs}s remaining.");
        }

        pvp.Enabled = false;
        return TextCommandResult.Success(Lang.Get("th3essentials:pvp-now-disabled"));
    }

    /* TODO: Need to handle the case where PVP is in progress when the server restarts.
     Players will be kicked before the server shutdown functionality is triggered,
     preventing us from just hooking into the Shutdown sequence to clear the values out.
     Unsure if there is a way to properly monitor for this? I would expect the answer to be "Yes" as there is a specific
     reason on the player kick but I don't see the documentation that defines what or where this is. */
}