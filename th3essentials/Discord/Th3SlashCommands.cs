using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Th3Essentials.Discord.Commands;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

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
                                GameDatabase gameDatabase = null;
                                string backupFileName = null;
                                //TODO where to trigger this
                                if (Th3Essentials.Config.BackupOnShutdown)
                                {
                                    discord.Sapi.Server.Config.Password = new Random().Next().ToString();
                                    discord.Sapi.Logger.Notification($"Temporary server password is: {discord.Sapi.Server.Config.Password}");
                                    foreach (IServerPlayer player in discord.Sapi.World.AllOnlinePlayers.Cast<IServerPlayer>())
                                    {
                                        player.Disconnect();
                                    }
                                    ServerMain server = (ServerMain)discord.Sapi.World;
                                    gameDatabase = new GameDatabase(discord.Sapi.Logger);

                                    _ = gameDatabase.ProbeOpenConnection(server.GetSaveFilename(), true, out _, out _, out _);
                                    FileInfo fileInfo = new FileInfo(gameDatabase.DatabaseFilename);
                                    long freeDiskSpace = ServerMain.xPlatInterface.GetFreeDiskSpace(fileInfo.DirectoryName);
                                    if (freeDiskSpace > fileInfo.Length){
                                        discord.Sapi.Logger.Debug($"SaveFileSize: {fileInfo.Length / 1000000 } MB, FreeDiskSpace: {freeDiskSpace / 1000000} MB");
                                    }

                                    string worldName = Path.GetFileName(discord.Sapi.WorldManager.CurrentWorldName);
                                    if (worldName.Length == 0)
                                    {
                                        worldName = "world";
                                    }
                                    backupFileName = worldName + "-" + $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}" + ".vcdbs";
                                }

                                Task.Run(() =>
                                {
                                    if (Th3Essentials.Config.BackupOnShutdown)
                                    {
                                        gameDatabase.CreateBackup(backupFileName);
                                    }
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
            string response = string.Empty;
            List<string> responseMult = null;
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
                            responseMult = Serverinfo.HandleSlashCommand(discord, commandInteraction);
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
            if (response != string.Empty)
            {
                _ = commandInteraction.RespondAsync(discord.ServerMsg(response), ephemeral: ephemeral, components: components);
            }
            else
            {
                if (responseMult.Count > 0)
                {
                    response = responseMult.FirstOrDefault();
                    responseMult.RemoveAt(0);
                    _ = commandInteraction.RespondAsync(discord.ServerMsg(response), ephemeral: ephemeral, components: components);
                    foreach (string res in responseMult)
                    {
                        _ = commandInteraction.FollowupAsync(discord.ServerMsg(res), ephemeral: ephemeral, components: components);
                    }
                }
            }
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