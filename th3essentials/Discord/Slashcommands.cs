using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace Th3Essentials.Discord
{
    public enum SlashCommands
    {
        Players, Date, RestartTime, SetChannel, Whitelist, AllowCharSelOnce, ModifyPermissions, Shutdown, Serverinfo, Stats, Admins
    }

    public class Th3SlashCommands
    {
        public static void CreateGuildCommands(DiscordSocketClient _client)
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

            List<SlashCommandOptionBuilder> playersOptions = new List<SlashCommandOptionBuilder>()
      {
          new SlashCommandOptionBuilder()
          {
              Name = "ping",
              Description = Lang.Get("th3essentials:slc-players-ping"),
              Type = ApplicationCommandOptionType.Boolean
          }
      };
            SlashCommandBuilder players = new SlashCommandBuilder
            {
                Name = SlashCommands.Players.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-players"),
                Options = playersOptions
            };
            _ = _client.Rest.CreateGuildCommand(players.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            SlashCommandBuilder date = new SlashCommandBuilder
            {
                Name = SlashCommands.Date.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-date")
            };
            _ = _client.Rest.CreateGuildCommand(date.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            SlashCommandBuilder restart = new SlashCommandBuilder
            {
                Name = SlashCommands.RestartTime.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-restart")
            };
            _ = _client.Rest.CreateGuildCommand(restart.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            List<SlashCommandOptionBuilder> channelOptions = new List<SlashCommandOptionBuilder>()
          {
            new SlashCommandOptionBuilder()
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
            _ = _client.Rest.CreateGuildCommand(setchannel.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            List<SlashCommandOptionBuilder> whitelistOptions = new List<SlashCommandOptionBuilder>()
                {
                    new SlashCommandOptionBuilder()
                    {
                        Name = "playername",
                        Description = Lang.Get("th3essentials:slc-whitelist-playername"),
                        Type = ApplicationCommandOptionType.String,
                        IsRequired = true
                    } ,
                    new SlashCommandOptionBuilder()
                    {
                        Name = "mode",
                        Description = Lang.Get("th3essentials:slc-whitelist-mode"),
                        Type = ApplicationCommandOptionType.Boolean,
                        IsRequired = true
                    },
                    new SlashCommandOptionBuilder()
                    {
                        Name = "time",
                        Description = Lang.Get("th3essentials:slc-whitelist-time"),
                        Type = ApplicationCommandOptionType.Integer,
                    },
                    new SlashCommandOptionBuilder()
                    {
                        Name = "timetype",
                        Description = Lang.Get("th3essentials:slc-whitelist-timetype"),
                        Type = ApplicationCommandOptionType.String,
                        Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                            new ApplicationCommandOptionChoiceProperties(){Name = "hours", Value = "hours"},
                            new ApplicationCommandOptionChoiceProperties(){Name = "days", Value = "days"},
                            new ApplicationCommandOptionChoiceProperties(){Name = "months", Value = "months"},
                            new ApplicationCommandOptionChoiceProperties(){Name = "years", Value = "years"}
                        }
                    },
                    new SlashCommandOptionBuilder()
                    {
                        Name = "reason",
                        Description = Lang.Get("th3essentials:slc-whitelist-reason"),
                        Type = ApplicationCommandOptionType.String
                    }
                };
            SlashCommandBuilder whitelist = new SlashCommandBuilder
            {
                Name = SlashCommands.Whitelist.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-whitelist"),
                Options = whitelistOptions
            };
            _ = _client.Rest.CreateGuildCommand(whitelist.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            List<SlashCommandOptionBuilder> charSelectOptions = new List<SlashCommandOptionBuilder>()
        {
          new SlashCommandOptionBuilder()
          {
            Name = "playername",
            Description = Lang.Get("th3essentials:slc-allowcharselonce-playername"),
            Type = ApplicationCommandOptionType.String,
            IsRequired = true
          }
        };
            SlashCommandBuilder allowcharselonce = new SlashCommandBuilder
            {
                Name = SlashCommands.AllowCharSelOnce.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-allowcharselonce"),
                Options = charSelectOptions
            };
            _ = _client.Rest.CreateGuildCommand(allowcharselonce.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            SlashCommandBuilder shutdown = new SlashCommandBuilder
            {
                Name = SlashCommands.Shutdown.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-shutdown")
            };
            _ = _client.Rest.CreateGuildCommand(shutdown.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            SlashCommandBuilder serverinfo = new SlashCommandBuilder
            {
                Name = SlashCommands.Serverinfo.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-serverinfo")
            };
            _ = _client.Rest.CreateGuildCommand(serverinfo.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            SlashCommandBuilder stats = new SlashCommandBuilder
            {
                Name = SlashCommands.Stats.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-stats")
            };
            _ = _client.Rest.CreateGuildCommand(stats.Build(), Th3Essentials.Config.DiscordConfig.GuildId);

            SlashCommandBuilder admins = new SlashCommandBuilder
            {
                Name = SlashCommands.Admins.ToString().ToLower(),
                Description = Lang.Get("th3essentials:slc-admins")
            };
            _ = _client.Rest.CreateGuildCommand(admins.Build(), Th3Essentials.Config.DiscordConfig.GuildId);
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

        internal static void HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
        {
            string response;
            MessageComponent components = null;
            if (Enum.TryParse(commandInteraction.Data.Name, true, out SlashCommands cmd))
            {
                switch (cmd)
                {
                    case SlashCommands.Players:
                        {
                            bool? ping = null;
                            foreach (SocketSlashCommandDataOption option in commandInteraction.Data.Options)
                            {
                                if (option.Name.Equals("ping"))
                                {
                                    ping = option.Value as bool?;
                                }
                            }
                            List<string> names = new List<string>();
                            foreach (IServerPlayer player in discord.Sapi.World.AllOnlinePlayers)
                            {
                                if (ping == true)
                                {
                                    names.Add($"{player.PlayerName} ({(int)(player.Ping * 1000)}ms)");
                                }
                                else
                                {
                                    names.Add(player.PlayerName);
                                }
                            }
                            response = names.Count == 0 ? Lang.Get("th3essentials:slc-players-none") : string.Join("\n", names);
                            break;
                        }
                    case SlashCommands.Date:
                        {
                            response = discord.Sapi.World.Calendar.PrettyDate();
                            break;
                        }
                    case SlashCommands.RestartTime:
                        {
                            if (Th3Essentials.Config.ShutdownTime != null)
                            {
                                TimeSpan restart = Th3Util.GetTimeTillRestart();
                                response = Lang.Get("th3essentials:slc-restart-resp", restart.Hours.ToString("D2"), restart.Minutes.ToString("D2"));
                            }
                            else
                            {
                                response = Lang.Get("th3essentials:slc-restart-disabled");
                            }
                            break;
                        }
                    case SlashCommands.SetChannel:
                        {
                            if (commandInteraction.User is SocketGuildUser guildUser)
                            {
                                if (guildUser.GuildPermissions.Administrator)
                                {
                                    SocketSlashCommandDataOption option = commandInteraction.Data.Options.First();
                                    if (option.Value is SocketTextChannel channel)
                                    {
                                        discord.Config.ChannelId = channel.Id;
                                        if (!discord.GetDiscordChannel())
                                        {
                                            discord.Sapi.Server.LogError($"Could not find channel with id: {discord.Config.ChannelId}");
                                            response = $"Could not find channel with id: {discord.Config.ChannelId}";
                                        }
                                        else
                                        {
                                            response = $"Channel was set to {channel.Name}";
                                        }
                                    }
                                    else
                                    {
                                        response = "Error: Channel needs to be a Text Channel";
                                    }
                                }
                                else
                                {
                                    response = "You need to have Administrator permissions to do that";
                                }
                            }
                            else
                            {
                                response = "Something went wrong: User was not a GuildUser";
                            }
                            break;
                        }
                    case SlashCommands.Whitelist:
                        {
                            if (commandInteraction.User is SocketGuildUser guildUser)
                            {
                                if (HasPermission(guildUser, discord.Config.ModerationRoles))
                                {
                                    string targetPlayer = null;
                                    bool? mode = null;
                                    long? time = null;
                                    string timetype = null;
                                    string reason = null;

                                    foreach (SocketSlashCommandDataOption option in commandInteraction.Data.Options)
                                    {
                                        switch (option.Name)
                                        {
                                            case "playername":
                                                {
                                                    targetPlayer = option.Value as string;
                                                    break;
                                                }
                                            case "mode":
                                                {
                                                    mode = option.Value as bool?;
                                                    break;
                                                }
                                            case "time":
                                                {
                                                    time = option.Value as long?;
                                                    break;
                                                }
                                            case "reason":
                                                {
                                                    reason = option.Value as string;
                                                    break;
                                                }
                                            case "timetype":
                                                {
                                                    timetype = option.Value as string;
                                                    break;
                                                }
                                            default:
                                                {
                                                    discord.Sapi.Logger.VerboseDebug("Something went wrong getting slc-whitelist option");
                                                    break;
                                                }
                                        }
                                    }

                                    if (targetPlayer != null && mode != null)
                                    {
                                        if (mode == true)
                                        {
                                            reason = reason ?? "";
                                            timetype = timetype ?? "years";
                                            int timenew = (int?)time ?? 50;
                                            DateTime datetime = DateTime.Now.ToLocalTime();
                                            switch (timetype)
                                            {
                                                case "hours":
                                                    {
                                                        datetime = datetime.AddHours(timenew);
                                                        break;
                                                    }
                                                case "days":
                                                    {
                                                        datetime = datetime.AddDays(timenew);
                                                        break;
                                                    }
                                                case "months":
                                                    {
                                                        datetime = datetime.AddMonths(timenew);
                                                        break;
                                                    }
                                                case "years":
                                                default:
                                                    {
                                                        datetime = datetime.AddYears(timenew);
                                                        break;
                                                    }
                                            }
                                            string name = guildUser.Nickname ?? guildUser.Username;

                                            GetPlayerUID(discord, targetPlayer, (playerUID) =>
                                              {
                                                  ((ServerMain)discord.Sapi.World).PlayerDataManager.WhitelistPlayer(targetPlayer, playerUID, name, reason, datetime);
                                              });
                                            response = $"Player is now whitelisted until {datetime}";
                                        }
                                        else
                                        {
                                            GetPlayerUID(discord, targetPlayer, (playerUID) =>
                                              {
                                                  _ = ((ServerMain)discord.Sapi.World).PlayerDataManager.UnWhitelistPlayer(targetPlayer, playerUID);
                                              });
                                            response = "Player is now removed from whitelist";
                                        }
                                    }
                                    else
                                    {
                                        response = "Playername or mode missing";
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
                            break;
                        }
                    case SlashCommands.AllowCharSelOnce:
                        {
                            if (commandInteraction.User is SocketGuildUser guildUser)
                            {
                                if (HasPermission(guildUser, discord.Config.ModerationRoles))
                                {
                                    SocketSlashCommandDataOption option = commandInteraction.Data.Options.First();
                                    if (option.Value is string playername)
                                    {
                                        IServerPlayerData player = discord.Sapi.PlayerData.GetPlayerDataByLastKnownName(playername);
                                        if (player != null)
                                        {
                                            IPlayer playerWoldData = discord.Sapi.World.PlayerByUid(player.PlayerUID);
                                            if (playerWoldData != null && SerializerUtil.Deserialize(playerWoldData.WorldData.GetModdata("createCharacter"), false))
                                            {
                                                playerWoldData.WorldData.SetModdata("createCharacter", SerializerUtil.Serialize(false));
                                                response = Lang.Get("Ok, player can now run .charsel (or rejoin the world) to change skin and character class once");
                                            }
                                            else
                                            {
                                                response = "Player is not online";
                                            }
                                        }
                                        else
                                        {
                                            response = "Could not find that player";
                                        }
                                    }
                                    else
                                    {
                                        response = "Error: playername needs to be set";
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
                            break;
                        }
                    case SlashCommands.ModifyPermissions:
                        {
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
                            break;
                        }
                    case SlashCommands.Shutdown:
                        {
                            if (commandInteraction.User is SocketGuildUser guildUser)
                            {
                                if (HasPermission(guildUser, discord.Config.ModerationRoles))
                                {
                                    ComponentBuilder builder = new ComponentBuilder().WithButton("Confirm", "shutdown-confirm");
                                    components = builder.Build();
                                    response = "Do you really want to shutdown the server?";
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
                    case SlashCommands.Serverinfo:
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append("Game version: ");
                            sb.AppendLine(GameVersion.OverallVersion);
                            sb.Append("Mods:");
                            foreach (Mod mod in discord.Sapi.ModLoader.Mods)
                            {
                                sb.AppendLine();
                                sb.Append($"  **{mod.Info.Name}** @ {mod.Info.Version} | {mod.Info.Side}");
                            }
                            response = sb.ToString();
                            break;
                        }
                    case SlashCommands.Stats:
                        {
                            if (commandInteraction.User is SocketGuildUser guildUser)
                            {
                                if (HasPermission(guildUser, discord.Config.ModerationRoles))
                                {
                                    StringBuilder stringBuilder = new StringBuilder();
                                    ServerMain server = (ServerMain)discord.Sapi.World;
                                    long upSeconds = server.totalUpTime.ElapsedMilliseconds / 1000;
                                    int upMinutes = 0;
                                    int upHours = 0;
                                    int upDays = 0;
                                    if (upSeconds > 60)
                                    {
                                        upMinutes = (int)(upSeconds / 60);
                                        upSeconds -= 60 * upMinutes;
                                    }
                                    if (upMinutes > 60)
                                    {
                                        upHours = upMinutes / 60;
                                        upMinutes -= 60 * upHours;
                                    }
                                    if (upHours > 24)
                                    {
                                        upDays = upHours / 24;
                                        upHours -= 24 * upDays;
                                    }
                                    stringBuilder.Append("Version: ");
                                    stringBuilder.AppendLine(GameVersion.OverallVersion);
                                    stringBuilder.AppendLine($"Uptime: {upDays} days, {upHours} hours, {upMinutes} minutes, {upSeconds} seconds");
                                    stringBuilder.AppendLine($"Players online: {server.Clients.Count} / {server.Config.MaxClients}");

                                    int activeEntities = 0;
                                    foreach (KeyValuePair<long, Entity> loadedEntity in server.LoadedEntities)
                                    {
                                        if (loadedEntity.Value.State != EnumEntityState.Inactive)
                                        {
                                            activeEntities++;
                                        }
                                    }
                                    stringBuilder.AppendLine($"Memory usage: {decimal.Round(GC.GetTotalMemory(forceFullCollection: false) / 1048576, 2)} Mb");
                                    StatsCollection statsCollection = server.StatsCollector[GameMath.Mod(server.StatsCollectorIndex - 1, server.StatsCollector.Length)];

                                    if (statsCollection.ticksTotal > 0)
                                    {
                                        stringBuilder.AppendLine($"Last 2s Average Tick Time: {decimal.Round(statsCollection.tickTimeTotal / (decimal)statsCollection.ticksTotal, 2)} ms");
                                        stringBuilder.AppendLine($"Last 2s Ticks/s: {decimal.Round((decimal)(statsCollection.ticksTotal / 2.0), 2)}");
                                        stringBuilder.AppendLine($"Last 10 ticks (ms): {string.Join(", ", statsCollection.tickTimes)}");
                                    }
                                    stringBuilder.AppendLine($"Loaded chunks: {discord.Sapi.World.LoadedChunkIndices.Count()}");
                                    stringBuilder.AppendLine($"Loaded entities: {server.LoadedEntities.Count} ({activeEntities} active)");
                                    stringBuilder.Append($"Network: {decimal.Round((decimal)(statsCollection.statTotalPackets / 2.0), 2)} Packets/s or {decimal.Round((decimal)(statsCollection.statTotalPacketsLength / 2048.0), 2, MidpointRounding.AwayFromZero) } Kb/s");
                                    response = stringBuilder.ToString();
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
                    case SlashCommands.Admins:
                        {
                            response = Th3Util.GetAdmins(discord.Sapi);
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
            _ = commandInteraction.RespondAsync(discord.ServerMsg(response), ephemeral: discord.Config.UseEphermalCmdResponse, components: components);
        }

        private static void GetPlayerUID(Th3Discord discord, string targetPlayer, Action<string> OnHavePlayerUid)
        {
            IServerPlayerData player = discord.Sapi.PlayerData.GetPlayerDataByLastKnownName(targetPlayer);
            if (player == null)
            {
                AuthServerComm.ResolvePlayerName(targetPlayer, (result, playeruid) => discord.Sapi.Event.EnqueueMainThreadTask(() =>
                {
                    if (result == EnumServerResponse.Good)
                    {
                        OnHavePlayerUid(playeruid);
                    }
                }, "th3discord-whitelist-getuid"));
            }
            else
            {
                OnHavePlayerUid(player.PlayerUID);
            }
        }

        private static bool HasPermission(SocketGuildUser guildUser, List<ulong> moderationRoles)
        {
            return guildUser.GuildPermissions.Administrator || guildUser.Roles.Select(r => r.Id).ToArray().Intersect(moderationRoles).Count() > 0;
        }
    }
}