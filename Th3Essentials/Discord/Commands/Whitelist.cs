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

public abstract class Whitelist
{
    public static SlashCommandProperties CreateCommand()
    {
        var whitelistOptions = new List<SlashCommandOptionBuilder>()
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

        var whitelist = new SlashCommandBuilder
        {
            Name = SlashCommands.Whitelist.ToString().ToLower(),
            Description = Lang.Get("th3essentials:slc-whitelist"),
            Options = whitelistOptions
        };
        return whitelist.Build();
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
                    discord.Sapi.Logger.VerboseDebug("Something went wrong getting slc-whitelist option");
                    break;
                }
            }
        }

        if (targetPlayer == null || mode == null)
            return "Playername or mode missing";
        
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
            var name = guildUser.DisplayName;
            var playerUid = await GetPlayerUid(discord.Sapi, targetPlayer);
            
            if (playerUid == null)
                return $"Could not find player with name: {targetPlayer}";
            
            ((ServerMain)discord.Sapi.World).PlayerDataManager.WhitelistPlayer(targetPlayer, playerUid, name, reason, datetime);
            discord.Sapi.Logger.Audit($"{guildUser.DisplayName}({guildUser.Id}) whitelisted {targetPlayer} for {datetime.ToString(CultureInfo.InvariantCulture)}.");

            return $"{targetPlayer} is now whitelisted until {datetime}";
        }
        else
        {
            var playerUid = await GetPlayerUid(discord.Sapi, targetPlayer);
            if (playerUid == null)
                return $"Could not find player with name: {targetPlayer}";
            
            _ = ((ServerMain)discord.Sapi.World).PlayerDataManager.UnWhitelistPlayer(targetPlayer, playerUid);
            return $"{targetPlayer} is now removed from whitelist";
        }
    }

    private static async Task<string?> GetPlayerUid(ICoreServerAPI sapi, string targetPlayer)
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