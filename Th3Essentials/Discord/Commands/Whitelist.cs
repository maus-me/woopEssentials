using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Th3Essentials.Discord.Commands
{
    public abstract class Whitelist
    {
        public static SlashCommandProperties CreateCommand()
        {
            List<SlashCommandOptionBuilder> whitelistOptions = new List<SlashCommandOptionBuilder>()
            {
                new()
                {
                    Name = "playername",
                    Description = Lang.Get("th3essentials:slc-whitelist-playername"),
                    Type = ApplicationCommandOptionType.String,
                    IsRequired = true
                } ,
                new()
                {
                    Name = "mode",
                    Description = Lang.Get("th3essentials:slc-whitelist-mode"),
                    Type = ApplicationCommandOptionType.Boolean,
                    IsRequired = true
                },
                new()
                {
                    Name = "time",
                    Description = Lang.Get("th3essentials:slc-whitelist-time"),
                    Type = ApplicationCommandOptionType.Integer,
                },
                new()
                {
                    Name = "timetype",
                    Description = Lang.Get("th3essentials:slc-whitelist-timetype"),
                    Type = ApplicationCommandOptionType.String,
                    Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                        new(){Name = "hours", Value = "hours"},
                        new(){Name = "days", Value = "days"},
                        new(){Name = "months", Value = "months"},
                        new(){Name = "years", Value = "years"}
                    }
                },
                new()
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
            return whitelist.Build();
        }

        public async static Task<string> HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
        {
            if (commandInteraction.User is SocketGuildUser guildUser)
            {
                if (Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
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
                            var name = guildUser.DisplayName;
                            var playerUid = await GetPlayerUID(discord.Sapi, targetPlayer);
                            if (playerUid != null)
                            {
                                ((ServerMain)discord.Sapi.World).PlayerDataManager.WhitelistPlayer(targetPlayer, playerUid, name, reason, datetime);
                                return $"{targetPlayer} is now whitelisted until {datetime}";
                            }
                            else
                            {
                                return $"Could not find player with name: {targetPlayer}";
                            }
                        }
                        else
                        {
                            string playerUID = await GetPlayerUID(discord.Sapi, targetPlayer);
                            if (playerUID != null)
                            {
                                _ = ((ServerMain)discord.Sapi.World).PlayerDataManager.UnWhitelistPlayer(targetPlayer, playerUID);
                                return $"{targetPlayer} is now removed from whitelist";
                            }
                            else
                            {
                                return $"Could not find player with name: {targetPlayer}";
                            }
                        }
                    }
                    else
                    {
                        return "Playername or mode missing";
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


        private async static Task<string> GetPlayerUID(ICoreServerAPI Sapi, string targetPlayer)
        {
            IServerPlayerData player = Sapi.PlayerData.GetPlayerDataByLastKnownName(targetPlayer);
            if (player == null)
            {
                using (HttpClient client = new HttpClient())
                {
                    List<KeyValuePair<string, string>> bodydata = new List<KeyValuePair<string, string>>
                    {
                        new("playername", targetPlayer)
                    };
                    FormUrlEncodedContent body = new FormUrlEncodedContent(bodydata);
                    HttpResponseMessage result = await client.PostAsync("https://auth.vintagestory.at/resolveplayername", body);
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        string responseData = await result.Content.ReadAsStringAsync();
                        ResolveResponse resolveResponse = JsonConvert.DeserializeObject<ResolveResponse>(responseData);
                        return resolveResponse.playeruid;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return player.PlayerUID;
            }
        }
    }
}