using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Th3Essentials.Discord.Commands;

public abstract class AllowCharSelOnce
{
    public static SlashCommandProperties CreateCommand()
    {
        List<SlashCommandOptionBuilder> charSelectOptions = new List<SlashCommandOptionBuilder>()
        {
            new()
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
        return allowcharselonce.Build();
    }

    public static string HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
    {
        if (commandInteraction.User is SocketGuildUser guildUser)
        {
            if (Th3SlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
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
                            return Lang.Get("Ok, player can now run .charsel (or rejoin the world) to change skin and character class once");
                        }
                        else
                        {
                            return "Player is not online";
                        }
                    }
                    else
                    {
                        return "Could not find that player";
                    }
                }
                else
                {
                    return "Error: playername needs to be set";
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
}