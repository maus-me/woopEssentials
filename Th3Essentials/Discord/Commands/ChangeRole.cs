using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Discord.Commands;

public abstract class ChangeRole
{
    public static SlashCommandProperties CreateCommand(ICoreServerAPI sapi)
    {
        var roles = new SlashCommandOptionBuilder()
        {
            Name = "rolecode",
            Description = Lang.Get("th3essentials:slc-changerole-rolecode"),
            Type = ApplicationCommandOptionType.String,
            IsRequired = true
        };

        var roleOptions = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "playername",
                Description = Lang.Get("th3essentials:slc-changerole-playername"),
                Type = ApplicationCommandOptionType.String,
                IsRequired = true
            },
            roles
        };
        var role = new SlashCommandBuilder
        {
            Name = SlashCommands.ChangeRole.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-changerole-role"),
            Options = roleOptions
        };
        return role.Build();
    }

    public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
    {
        if (commandInteraction.User is not SocketGuildUser guildUser)
        {
            return "Something went wrong: User was not a GuildUser";
        }

        if (!Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
        {
            return "You do not have permissions to do that";
        }

        var option = commandInteraction.Data.Options.First();
        if (option.Value is not string playername)
        {
            return "Error: playername needs to be set";
        }

        var role = commandInteraction.Data.Options.ElementAtOrDefault(1);
        if (role?.Value is not string roleCode)
        {
            return "Error: rolename needs to be set";
        }

        if (!discord.Sapi.Server.Config.Roles.Any(r => r.Code.Equals(roleCode)))
        {
            return "Error: Role not found in available roles";
        }

        var player = discord.Sapi.PlayerData.GetPlayerDataByLastKnownName(playername);
        if (player == null)
        {
            return "Could not find that player";
        }

        var cmdArgs = new TextCommandCallingArgs
        {
            Caller = new Caller()
            {
                Type = EnumCallerType.Console,
                CallerRole = "admin",
                CallerPrivileges = new[] { "grantrevoke", "chat","root" },
                FromChatGroupId = GlobalConstants.ConsoleGroup
            },
            RawArgs = new CmdArgs($"{playername} role {roleCode}")
        };
        discord.Sapi.ChatCommands["player"].Execute(cmdArgs, (args) =>
        {
            if (args.Status == EnumCommandStatus.Success)
            {
                discord.Sapi.Logger.Audit($"Discord user {guildUser.DisplayName}({guildUser.Id}) changed {playername}'s role to {roleCode}");
            }
        });
        return Lang.Get($"Ok, players roles is now set to {roleCode}");
    }
}