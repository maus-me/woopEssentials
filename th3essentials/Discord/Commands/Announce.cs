using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public class Announce
    {
        public static void CreateCommand(DiscordSocketClient _client)
        {
            List<SlashCommandOptionBuilder> announceOptions = new List<SlashCommandOptionBuilder>()
            {
                new SlashCommandOptionBuilder()
                {
                    Name = "message",
                    Description = Lang.Get("th3essentials:slc-announce-msg"),
                    Type = ApplicationCommandOptionType.String,
                    IsRequired = true
                },
                new SlashCommandOptionBuilder()
                {
                    Name = "show",
                    Description = Lang.Get("th3essentials:slc-announce-showindiscord"),
                    Type = ApplicationCommandOptionType.Boolean,
                    IsRequired = true
                },
                new SlashCommandOptionBuilder()
                {
                    Name = "color",
                    Description = Lang.Get("th3essentials:slc-announce-color"),
                    Type = ApplicationCommandOptionType.String,
                    Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                        new ApplicationCommandOptionChoiceProperties(){Name = "Black", Value = "Black"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Blue", Value = "Blue"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "BlueViolet", Value = "BlueViolet"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Cyan", Value = "Cyan"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "DarkBlue", Value = "DarkBlue"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "DarkCyan", Value = "DarkCyan"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "DarkGray", Value = "DarkGray"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "DarkGreen", Value = "DarkGreen"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "DarkMagenta", Value = "DarkMagenta"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "DarkOrange", Value = "DarkOrange"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "DarkRed", Value = "DarkRed"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "DarkViolet", Value = "DarkViolet"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Gold", Value = "Gold"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Green", Value = "Green"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Lime", Value = "Lime"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Magenta", Value = "Magenta"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Orange", Value = "Orange"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Pink", Value = "Pink"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Purple", Value = "Purple"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Red", Value = "Red"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Silver", Value = "Silver"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Violet", Value = "Violet"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "White", Value = "White"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "Yellow", Value = "Yellow"},
                    }
                }
            };
            SlashCommandBuilder announce = new SlashCommandBuilder
            {
                Name = SlashCommands.Announce.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-announce"),
                Options = announceOptions
            };
            _ = _client.Rest.CreateGuildCommand(announce.Build(), Th3Essentials.Config.DiscordConfig.GuildId);
        }

        public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction, ref bool ephemeral)
        {
            if (commandInteraction.User is SocketGuildUser guildUser)
            {
                if (Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
                {
                    string message = null;
                    bool? show = null;
                    string color = "orange";
                    foreach (SocketSlashCommandDataOption option in commandInteraction.Data.Options)
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
                    if (message != null && show != null)
                    {
                        ephemeral = !(bool)show;
                        discord.Sapi.Logger.Event($"{guildUser.DisplayName}#{guildUser.Discriminator} announced: {message}.");
                        discord.Sapi.BroadcastMessageToAllGroups($"<strong><font color=\"{color}\">{message}</font></strong>", EnumChatType.AllGroups);
                        return message;
                    }
                    else
                    {
                        return "Something went wrong: missing argument";
                    }
                }
                else
                {
                    return "You do not have permissions to do that";
                }
            }
            else
            {
                return "Something went wrong: User was not a GuildUser";
            }
        }
    }
}