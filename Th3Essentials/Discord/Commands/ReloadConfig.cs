using System;
using Discord;
using Discord.WebSocket;
using Th3Essentials.Config;
using Vintagestory.API.Config;

namespace Th3Essentials.Discord.Commands
{
    public abstract class ReloadConfig
    {
        public static SlashCommandProperties CreateCommand()
        {
            SlashCommandBuilder reloadConfig = new SlashCommandBuilder
            {
                Name = SlashCommands.ReloadConfig.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-reload")
            };
            return reloadConfig.Build();
        }

        public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
        {
            if (!(commandInteraction.User is SocketGuildUser guildUser))
            {
                return "You do not have permissions to do that";
            }

            if (!Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
            {
                return "You do not have permissions to do that";
            }

            try
            {
                var configTemp = discord.Sapi.LoadModConfig<Th3Config>(Th3Essentials._configFile);
                Th3Essentials.Config.Reload(configTemp);
                Th3Essentials.LoadRestartTime(DateTime.Now);
                return "Config reloaded";
            }
            catch (Exception e)
            {
                discord.Sapi.Logger.Error("Error reloading Th3Config: ", e.ToString());
                return "Error reloading Th3Config see server log";
            }
        }
    }
}