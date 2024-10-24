using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Th3Essentials.Discord.Commands;

public abstract class Ban
{
    public static SlashCommandProperties CreateCommand()
    {
        var banOptions = new List<SlashCommandOptionBuilder>()
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
                Description = Lang.Get("th3essentials:slc-ban-mode"),
                Type = ApplicationCommandOptionType.Boolean,
                IsRequired = true
            },
            new()
            {
                Name = "time",
                Description = Lang.Get("th3essentials:slc-ban-time"),
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
                Description = Lang.Get("th3essentials:slc-ban-reason"),
                Type = ApplicationCommandOptionType.String
            }
        };

        var ban = new SlashCommandBuilder
        {
            Name = SlashCommands.Ban.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-ban"),
            Options = banOptions
        };
        return ban.Build();
    }

    public static async Task<string> HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
    {
        if (commandInteraction.User is not SocketGuildUser guildUser)
            return "Something went wrong: User was not a GuildUser";

        if (!Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
            return "You do not have permissions to do that";
        
        string? targetPlayer = null;
        bool? mode = null;
        long? time = null;
        string? timetype = null;
        string? reason = null;

        foreach (var option in commandInteraction.Data.Options)
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
                    discord.Sapi.Logger.VerboseDebug("Something went wrong getting slc-whitelist/ban option");
                    break;
                }
            }
        }

        if (targetPlayer == null || mode == null) return "Playername or mode missing";
        
        var playerDataManager = ((ServerMain)discord.Sapi.World).PlayerDataManager;
        if (mode == true)
        {
            reason ??= "";
            timetype ??= "years";
            var timenew = (int?)time ?? 50;
            var datetime = DateTime.Now.ToLocalTime();
            datetime = timetype switch
            {
                "hours" => datetime.AddHours(timenew),
                "days" => datetime.AddDays(timenew),
                "months" => datetime.AddMonths(timenew),
                _ => datetime.AddYears(timenew)
            };
            var playerUid = await GetPlayerUid(discord.Sapi, targetPlayer);
            
            if (playerUid == null)
                return $"Could not find player with name: {targetPlayer}";
            
            BanPlayer(playerDataManager, guildUser.DisplayName, playerUid, targetPlayer, reason, datetime);
            discord.Sapi.Logger.Audit($"{guildUser.DisplayName}({guildUser.Id}) banned {targetPlayer} for {datetime.ToString(CultureInfo.InvariantCulture)}.");

            return $"{targetPlayer} is now banned until {datetime}";

        }
        else
        {
            var playerUid = await GetPlayerUid(discord.Sapi, targetPlayer);
            
            if (playerUid == null)
                return $"Could not find player with name: {targetPlayer}";
            
            UnbanPlayer(playerDataManager, guildUser.DisplayName, playerUid, targetPlayer);
            discord.Sapi.Logger.Audit($"{guildUser.DisplayName}({guildUser.Id}) unbanned {targetPlayer}.");

            return $"{targetPlayer} is now removed from banned players";

        }
    }

    private static void BanPlayer(PlayerDataManager playerDataManager, string byDiscordUser, string playerUid, string targetPlayer,
        string reason, DateTime datetime)
    {
        var entry = playerDataManager.GetPlayerBan(byDiscordUser, playerUid);

        if (entry == null)
        {
            playerDataManager.BannedPlayers.Add(new PlayerEntry()
            {
                PlayerName = targetPlayer,
                IssuedByPlayerName = byDiscordUser,
                PlayerUID = playerUid,
                Reason = reason,
                UntilDate = datetime
            });

            ServerMain.Logger.Audit("{0} was banned by {1} until {2}. Reason: {3}", targetPlayer, byDiscordUser, datetime, reason);

        } else
        {
            entry.Reason = reason;
            entry.UntilDate = datetime;

            ServerMain.Logger.Audit("Existing player ban of {0} updated by {1}. Now until {2}, Reason: {3}", targetPlayer, byDiscordUser, datetime, reason);
        }

        playerDataManager.bannedListDirty = true;
    }
    
    private static void UnbanPlayer(PlayerDataManager playerDataManager, string byDiscordUser, string playerUid, string targetPlayer)
    {
        var entry = playerDataManager.GetPlayerBan(targetPlayer, playerUid);

        if (entry == null) return;
        
        playerDataManager.BannedPlayers.Remove(entry);
        playerDataManager.bannedListDirty = true;
        ServerMain.Logger.Audit("{0} was unbanned by {1}.", targetPlayer, byDiscordUser);
    }

    public static async Task<string?> GetPlayerUid(ICoreServerAPI sapi, string targetPlayer)
    {
        var player = sapi.PlayerData.GetPlayerDataByLastKnownName(targetPlayer);
        
        if (player != null) return player.PlayerUID;
        
        using var client = new HttpClient();
        var bodydata = new List<KeyValuePair<string, string>>
        {
            new("playername", targetPlayer)
        };
        
        var body = new FormUrlEncodedContent(bodydata);
        var result = await client.PostAsync("https://auth.vintagestory.at/resolveplayername", body);
        
        if (result.StatusCode != HttpStatusCode.OK) return null;
        
        var responseData = await result.Content.ReadAsStringAsync();
        var resolveResponse = JsonConvert.DeserializeObject<ResolveResponse>(responseData);
        return resolveResponse?.playeruid;
    }
}