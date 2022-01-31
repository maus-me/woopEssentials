using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public class ModifyPermissions
    {
        public static void CreateCommand(DiscordSocketClient _client)
        {
            List<SlashCommandOptionBuilder> modifypermissionsOptions = new List<SlashCommandOptionBuilder>()
            {
                new SlashCommandOptionBuilder()
                {
                    Name = "mode",
                    Description = Lang.Get("th3essentials:slc-modifypermissions-mode"),
                    Type = ApplicationCommandOptionType.String,
                    Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                        new ApplicationCommandOptionChoiceProperties(){Name = "add", Value = "add"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "remove", Value = "remove"},
                        new ApplicationCommandOptionChoiceProperties(){Name = "clear", Value = "clear"}
                    },
                    IsRequired = true,
                },
                new SlashCommandOptionBuilder()
                {
                Name = "role",
                Description = Lang.Get("th3essentials:slc-modifypermissions"),
                Type = ApplicationCommandOptionType.Role
                }
            };
            SlashCommandBuilder modifypermissions = new SlashCommandBuilder
            {
                Name = SlashCommands.ModifyPermissions.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-modifypermissions"),
                Options = modifypermissionsOptions
            };
            _ = _client.Rest.CreateGuildCommand(modifypermissions.Build(), Th3Essentials.Config.DiscordConfig.GuildId);
        }

        public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
        {
            string response;
            if (commandInteraction.User is SocketGuildUser guildUser)
            {
                if (guildUser.GuildPermissions.Administrator)
                {
                    SocketRole role = null;
                    string mode = null;
                    foreach (SocketSlashCommandDataOption option in commandInteraction.Data.Options)
                    {
                        switch (option.Name)
                        {
                            case "role":
                                {
                                    role = option.Value as SocketRole;
                                    break;
                                }
                            case "mode":
                                {
                                    mode = option.Value as string;
                                    break;
                                }
                            default: { break; }
                        }
                    }
                    switch (mode)
                    {
                        case "add":
                            {
                                if (role != null)
                                {
                                    if (discord.Config.ModerationRoles == null)
                                    {
                                        discord.Config.ModerationRoles = new List<ulong>();
                                    }
                                    discord.Config.ModerationRoles.Add(role.Id);
                                    Th3Essentials.Config.MarkDirty();
                                    response = $"Added role: {role.Name}";
                                }
                                else
                                {
                                    response = "Invalid role";
                                }
                                break;
                            }
                        case "remove":
                            {
                                if (role != null)
                                {
                                    if (discord.Config.ModerationRoles != null)
                                    {
                                        if (discord.Config.ModerationRoles.Remove(role.Id))
                                        {
                                            Th3Essentials.Config.MarkDirty();
                                            response = $"Removed role: {role.Name}";
                                        }
                                        else
                                        {
                                            response = "Role had not permissions, nothing to remove";
                                        }
                                    }
                                    else
                                    {
                                        response = "Nothing to remove";
                                    }
                                }
                                else
                                {
                                    response = "Invalid role";
                                }
                                break;
                            }
                        case "clear":
                            {
                                if (discord.Config.ModerationRoles != null)
                                {
                                    discord.Config.ModerationRoles.Clear();
                                    Th3Essentials.Config.MarkDirty();
                                    response = "All moderation roles removed";
                                }
                                else
                                {
                                    response = "Nothing to remove";
                                }
                                break;
                            }
                        default:
                            {
                                response = $"Error: Mode option invalid: {mode}";
                                break;
                            }
                    }
                }
                else
                {
                    response = "You do not have permissions to do that";
                }
            }
            else
            {
                response = "Something went wrong: User was not a GuildUser";
            }
            return response;
        }
    }
}