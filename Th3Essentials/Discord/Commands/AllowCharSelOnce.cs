using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Vintagestory.API.Config;
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
                Description = Lang.Get("woopessentials:slc-allowcharselonce-playername"),
                Type = ApplicationCommandOptionType.String,
                IsRequired = true
            }
        };
        var allowcharselonce = new SlashCommandBuilder
        {
            Name = SlashCommands.AllowCharSelOnce.ToString().ToLower(),
            Description = Lang.Get("woopessentials:slc-allowcharselonce"),
            Options = charSelectOptions
        };
        return allowcharselonce.Build();
    }

    public static string HandleSlashCommand(WoopDiscord discord, SocketSlashCommand commandInteraction)
    {
        if (commandInteraction.User is SocketGuildUser guildUser)
        {
            if (WoopSlashCommands.HasPermission(guildUser, discord.Config.ModerationRoles))
            {
                var option = commandInteraction.Data.Options.First();
                if (option.Value is string playername)
                {
                    var player = discord.Sapi.PlayerData.GetPlayerDataByLastKnownName(playername);
                    if (player != null)
                    {
                        var playerWoldData = discord.Sapi.World.PlayerByUid(player.PlayerUID);
                        if (playerWoldData != null && SerializerUtil.Deserialize(playerWoldData.WorldData.GetModdata("createCharacter"), false))
                        {
                            playerWoldData.WorldData.SetModdata("createCharacter", SerializerUtil.Serialize(false));
                            discord.Sapi.Logger.Audit($"{guildUser.DisplayName}({guildUser.Id}) granted charsel to {playername}.");
                            return Lang.Get("Ok, player can now run .charsel (or rejoin the world) to change skin and character class once");
                        }

                        return "Player is not online";
                    }

                    return "Could not find that player";
                }

                return "Error: playername needs to be set";
            }

            return "You do not have permissions to do that";
        }

        return "Something went wrong: User was not a GuildUser";
    }
}