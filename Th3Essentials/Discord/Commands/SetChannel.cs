using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public abstract class SetChannel
    {
        public static SlashCommandProperties CreateCommand()
        {
            List<SlashCommandOptionBuilder> channelOptions = new List<SlashCommandOptionBuilder>()
            {
                new()
                {
                Name = "channel",
                Description = Lang.Get("th3essentials:slc-setchannel"),
                Type = ApplicationCommandOptionType.Channel,
                IsRequired = true
                }
            };
            SlashCommandBuilder setchannel = new SlashCommandBuilder
            {
                Name = SlashCommands.SetChannel.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-setchannel"),
                Options = channelOptions
            };
            return setchannel.Build();
        }

        public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
        {
            if (commandInteraction.User is SocketGuildUser guildUser)
            {
                if (guildUser.GuildPermissions.Administrator)
                {
                    SocketSlashCommandDataOption option = commandInteraction.Data.Options.First();
                    if (option.Value is SocketTextChannel channel)
                    {
                        discord.Config.ChannelId = channel.Id;
                        Th3Essentials.Config.MarkDirty();
                        if (!discord.GetDiscordChannel())
                        {
                            discord.Sapi.Server.LogError($"Could not find channel with id: {discord.Config.ChannelId}");
                            return $"Could not find channel with id: {discord.Config.ChannelId}";
                        }
                        else
                        {
                            return $"Channel was set to {channel.Name}";
                        }
                    }
                    else
                    {
                        return "Error: Channel needs to be a Text Channel";
                    }
                }
                else
                {
                    return "You need to have Administrator permissions to do that";
                }
            }
            else
            {
                return "Something went wrong: User was not a GuildUser";
            }
        }
    }
}