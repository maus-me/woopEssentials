using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Th3Essentials.Discord.Commands;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Th3Essentials.Discord;

public enum SlashCommands
{
    Players, Date, RestartTime, SetChannel, Whitelist, AllowCharSelOnce, ModifyPermissions, Shutdown, Serverinfo, Stats, Admins, Auth, Announce,
    ReloadConfig, ChangeRole, Ban, Kick
}

public abstract class Th3SlashCommands
{
    public static void CreateGuildCommands(DiscordSocketClient client, ICoreServerAPI sapi)
    {
        var commands = new ApplicationCommandProperties[]
        {
            Players.CreateCommand(),
            Date.CreateCommand(),
            RestartTime.CreateCommand(),
            SetChannel.CreateCommand(),
            Whitelist.CreateCommand(),
            AllowCharSelOnce.CreateCommand(),
            ModifyPermissions.CreateCommand(),
            Shutdown.CreateCommand(),
            Serverinfo.CreateCommand(),
            Stats.CreateCommand(),
            Admins.CreateCommand(),
            Auth.CreateCommand(),
            Announce.CreateCommand(),
            ReloadConfig.CreateCommand(),
            Ban.CreateCommand(),
            Kick.CreateCommand(),
            ChangeRole.CreateCommand(sapi)
        };
        var created = client.Rest.BulkOverwriteGuildCommands(commands, Th3Discord.Instance.Config.GuildId).GetAwaiter().GetResult();
            
        sapi.Logger.Notification($"Discord Slashcommands created: {string.Join(", ", created.Select(c => c.Name))}");
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

    internal static async void HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
    {
        var response = string.Empty;
        List<string>? responseMult = null;
        var ephemeral = discord.Config.UseEphermalCmdResponse;
        MessageComponent? components = null;
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
                case SlashCommands.ReloadConfig:
                {
                    response = ReloadConfig.HandleSlashCommand(discord, commandInteraction);
                    break;
                }
                case SlashCommands.Ban:
                {
                    response = await Ban.HandleSlashCommand(discord, commandInteraction);
                    break;
                }
                case SlashCommands.Kick:
                {
                    response = await Kick.HandleSlashCommand(discord, commandInteraction);
                    break;
                }
                case SlashCommands.ChangeRole:
                {
                    response = ChangeRole.HandleSlashCommand(discord, commandInteraction);
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
            if (!(responseMult?.Count > 0)) return;
                
            response = responseMult.FirstOrDefault()!;
            responseMult.RemoveAt(0);
            _ = commandInteraction.RespondAsync(discord.ServerMsg(response), ephemeral: ephemeral);
            foreach (var res in responseMult)
            {
                _ = commandInteraction.FollowupAsync(discord.ServerMsg(res), ephemeral: ephemeral);
            }
        }
    }

    public static bool HasPermission(SocketGuildUser guildUser, List<ulong>? moderationRoles)
    {
        if (guildUser.GuildPermissions.Administrator)
        {
            return true;
        }

        return moderationRoles != null && guildUser.Roles.Select(r => r.Id).ToArray().Intersect(moderationRoles).Any();
    }

    public static async Task<string?> GetPlayerUid(ICoreServerAPI sapi, string targetPlayer)
    {
        var player = sapi.PlayerData.GetPlayerDataByLastKnownName(targetPlayer);

        if (player == null)
        {
            foreach (var p in sapi.World.AllPlayers)
            {
                if (p.PlayerUID.Equals(targetPlayer, StringComparison.OrdinalIgnoreCase))
                {
                    return p.PlayerUID;
                }
            }
        }
        
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