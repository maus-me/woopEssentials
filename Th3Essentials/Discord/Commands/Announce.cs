using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands;

public abstract class Announce
{
    public static SlashCommandProperties CreateCommand()
    {
        List<SlashCommandOptionBuilder> announceOptions = new List<SlashCommandOptionBuilder>()
        {
            new()
            {
                Name = "message",
                Description = Lang.Get("woopessentials:slc-announce-msg"),
                Type = ApplicationCommandOptionType.String,
                IsRequired = true
            },
            new()
            {
                Name = "show",
                Description = Lang.Get("woopessentials:slc-announce-showindiscord"),
                Type = ApplicationCommandOptionType.Boolean,
                IsRequired = true
            },
            new()
            {
                Name = "color",
                Description = Lang.Get("woopessentials:slc-announce-color"),
                Type = ApplicationCommandOptionType.String,
                Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                    new(){Name = "Black", Value = "Black"},
                    new(){Name = "Blue", Value = "Blue"},
                    new(){Name = "BlueViolet", Value = "BlueViolet"},
                    new(){Name = "Cyan", Value = "Cyan"},
                    new(){Name = "DarkBlue", Value = "DarkBlue"},
                    new(){Name = "DarkCyan", Value = "DarkCyan"},
                    new(){Name = "DarkGray", Value = "DarkGray"},
                    new(){Name = "DarkGreen", Value = "DarkGreen"},
                    new(){Name = "DarkMagenta", Value = "DarkMagenta"},
                    new(){Name = "DarkOrange", Value = "DarkOrange"},
                    new(){Name = "DarkRed", Value = "DarkRed"},
                    new(){Name = "DarkViolet", Value = "DarkViolet"},
                    new(){Name = "Gold", Value = "Gold"},
                    new(){Name = "Green", Value = "Green"},
                    new(){Name = "Lime", Value = "Lime"},
                    new(){Name = "Magenta", Value = "Magenta"},
                    new(){Name = "Orange", Value = "Orange"},
                    new(){Name = "Pink", Value = "Pink"},
                    new(){Name = "Purple", Value = "Purple"},
                    new(){Name = "Red", Value = "Red"},
                    new(){Name = "Silver", Value = "Silver"},
                    new(){Name = "Violet", Value = "Violet"},
                    new(){Name = "White", Value = "White"},
                    new(){Name = "Yellow", Value = "Yellow"},
                }
            }
        };
        var announce = new SlashCommandBuilder
        {
            Name = SlashCommands.Announce.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-announce"),
            Options = announceOptions
        };
        return announce.Build();
    }

    public static string HandleSlashCommand(WoopDiscord discord, SocketSlashCommand commandInteraction, ref bool ephemeral)
    {
        if (commandInteraction.User is SocketGuildUser guildUser)
        {
            if (WoopSlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
            {
                string? message = null;
                bool? show = null;
                var color = "orange";
                foreach (var option in commandInteraction.Data.Options)
                {
                    switch (option.Name)
                    {
                        case "message":
                        {
                            message = option.Value as string;
                            break;
                        }
                        case "show":
                        {
                            show = option.Value as bool?;
                            break;
                        }
                        case "color":
                        {
                            color = option.Value as string;
                            break;
                        }
                    }
                }

                if (message == null || show == null)
                    return "Something went wrong: missing argument";
                
                var cleanMessage = message.Replace("<", "&lt;").Replace(">", "&gt;");
                ephemeral = !(bool)show;
                discord.Sapi.Logger.Audit($"{guildUser.DisplayName}({guildUser.Id}) announced: {cleanMessage}.");
                discord.Sapi.BroadcastMessageToAllGroups($"<strong><font color=\"{color}\">{cleanMessage}</font></strong>", EnumChatType.AllGroups);
                return cleanMessage;

            }

            return "You do not have permissions to do that";
        }

        return "Something went wrong: User was not a GuildUser";
    }
}