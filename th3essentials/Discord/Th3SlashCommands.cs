using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Th3Essentials.Discord.Commands;

namespace Th3Essentials.Discord
{
    public enum SlashCommands
    {
        Players, Date, RestartTime, SetChannel, Whitelist, AllowCharSelOnce, ModifyPermissions, Shutdown, Serverinfo, Stats, Admins, Auth, Announce
    }

    public class Th3SlashCommands
    {
        public static void CreateGuildCommands(DiscordSocketClient _client)
        {
            Players.CreateCommand(_client);
            Date.CreateCommand(_client);
            RestartTime.CreateCommand(_client);
            SetChannel.CreateCommand(_client);
            Whitelist.CreateCommand(_client);
            AllowCharSelOnce.CreateCommand(_client);
            ModifyPermissions.CreateCommand(_client);
            Shutdown.CreateCommand(_client);
            Serverinfo.CreateCommand(_client);
            Stats.CreateCommand(_client);
            Admins.CreateCommand(_client);
            Auth.CreateCommand(_client);
            Announce.CreateCommand(_client);
        }

        internal static void HandleButtonExecuted(Th3Discord discord, SocketMessageComponent component)
        {
            string response;
            switch (component.Data.CustomId)
            {
                case "shutdown-confirm":
                    {
                        if (component.User is SocketGuildUser guildUser)
                        {
                            if (HasPermission(guildUser, discord.Config.ModerationRoles))
                            {
                                Task.Run(() =>
                                {
                                    discord.Sapi.Server.ShutDown();
                                });
                                response = "Server is going to shutdown now.";
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
                        break;
                    }
                default:
                    {
                        response = "";
                        break;
                    }
            }
            component.RespondAsync(response, ephemeral: discord.Config.UseEphermalCmdResponse);
        }

        internal async static void HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
        {
            string response;
            bool ephemeral = discord.Config.UseEphermalCmdResponse;
            MessageComponent components = null;
            if (Enum.TryParse(commandInteraction.Data.Name, true, out SlashCommands cmd))
            {
                switch (cmd)
                {
                    case SlashCommands.Players:
                        {
                            response = Players.HandleSlashCommand(discord, commandInteraction);
                            break;
                        }
                    case SlashCommands.Date:
                        {
                            response = discord.Sapi.World.Calendar.PrettyDate();
                            break;
                        }
                    case SlashCommands.RestartTime:
                        {
                            response = RestartTime.HandleSlashCommand();
                            break;
                        }
                    case SlashCommands.SetChannel:
                        {
                            response = SetChannel.HandleSlashCommand(discord, commandInteraction);
                            break;
                        }
                    case SlashCommands.Whitelist:
                        {
                            response = await Whitelist.HandleSlashCommand(discord, commandInteraction);
                            break;
                        }
                    case SlashCommands.AllowCharSelOnce:
                        {
                            response = AllowCharSelOnce.HandleSlashCommand(discord, commandInteraction);
                            break;
                        }
                    case SlashCommands.ModifyPermissions:
                        {
                            response = ModifyPermissions.HandleSlashCommand(discord, commandInteraction);
                            break;
                        }
                    case SlashCommands.Shutdown:
                        {
                            response = Shutdown.HandleSlashCommand(discord, commandInteraction, ref components);
                            break;
                        }
                    case SlashCommands.Serverinfo:
                        {
                            response = Serverinfo.HandleSlashCommand(discord, commandInteraction);
                            break;
                        }
                    case SlashCommands.Stats:
                        {
                            response = Stats.HandleSlashCommand(discord, commandInteraction);
                            break;
                        }
                    case SlashCommands.Admins:
                        {
                            response = Th3Util.GetAdmins(discord.Sapi);
                            break;
                        }
                    case SlashCommands.Auth:
                        {
                            response = Auth.HandleSlashCommand(commandInteraction);
                            break;
                        }
                    case SlashCommands.Announce:
                        {
                            response = Announce.HandleSlashCommand(discord, commandInteraction, ref ephemeral);
                            break;
                        }
                    default:
                        {
                            response = "Unknown SlashCommand";
                            break;
                        }
                }
            }
            else
            {
                response = "Unknown SlashCommand";
            }
            _ = commandInteraction.RespondAsync(discord.ServerMsg(response), ephemeral: ephemeral, components: components);
        }

        public static bool HasPermission(SocketGuildUser guildUser, List<ulong> moderationRoles)
        {
            if (guildUser.GuildPermissions.Administrator)
            {
                return true;
            }
            else if (moderationRoles != null)
            {
                return guildUser.Roles.Select(r => r.Id).ToArray().Intersect(moderationRoles).Count() > 0;
            }
            return false;
        }
    }
}