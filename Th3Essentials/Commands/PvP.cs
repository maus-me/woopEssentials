﻿using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using System;
using System.Collections.Generic;
using Th3Essentials.Config;
using Th3Essentials.Systems;

namespace Th3Essentials.Commands;

internal class PvP : Command
{
    private Th3PlayerConfig _playerConfig = null!;

    private Th3Config _config = null!;

    private ICoreServerAPI _sapi = null!;

    internal override void Init(ICoreServerAPI api)
    {
        if (!Th3Essentials.Config.EnablePvPToggle) return;

        _sapi = api;
        _playerConfig = Th3Essentials.PlayerConfig;
        _config = Th3Essentials.Config;

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
}