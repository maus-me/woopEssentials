using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace Th3Essentials.Discordbot
{
  public enum SlashCommands
  {
    Players, Date, Restart, SetChannel, Whitelist, AllowCharSelOnce, ModifyPermissions
  }

  public class Th3SlashCommands
  {
    public static void CreateGuildCommands(DiscordSocketClient _client)
    {
      List<SlashCommandOptionBuilder> modifypermissionsOptions = new List<SlashCommandOptionBuilder>()
          {
            new SlashCommandOptionBuilder()
            {
              Name = "role",
              Description = Lang.Get("th3essentials:slc-modifypermissions"),
              Type = ApplicationCommandOptionType.Role,
              Required = true
            },
            new SlashCommandOptionBuilder()
            {
                Name = "mode",
                Description = Lang.Get("th3essentials:slc-modifypermissions-mode"),
                Type = ApplicationCommandOptionType.String,
                Choices = new List<ApplicationCommandOptionChoiceProperties>(){
                    new ApplicationCommandOptionChoiceProperties(){Name = "add", Value = "add"},
                    new ApplicationCommandOptionChoiceProperties(){Name = "remove", Value = "remove"},
                    new ApplicationCommandOptionChoiceProperties(){Name = "clear", Value = "clear"}
                }
            }
          };
      SlashCommandBuilder modifypermissions = new SlashCommandBuilder
      {
        Name = SlashCommands.ModifyPermissions.ToString().ToLower(),
        Description = Lang.Get("th3essentials:slc-modifypermissions"),
        Options = modifypermissionsOptions
      };
      _ = _client.Rest.CreateGuildCommand(modifypermissions.Build(), Th3Essentials.Config.GuildId);

      SlashCommandBuilder players = new SlashCommandBuilder
      {
        Name = SlashCommands.Players.ToString().ToLower(),
        Description = Lang.Get("th3essentials:slc-players")
      };
      _ = _client.Rest.CreateGuildCommand(players.Build(), Th3Essentials.Config.GuildId);

      SlashCommandBuilder date = new SlashCommandBuilder
      {
        Name = SlashCommands.Date.ToString().ToLower(),
        Description = Lang.Get("th3essentials:slc-date")
      };
      _ = _client.Rest.CreateGuildCommand(date.Build(), Th3Essentials.Config.GuildId);

      SlashCommandBuilder restart = new SlashCommandBuilder
      {
        Name = SlashCommands.Restart.ToString().ToLower(),
        Description = Lang.Get("th3essentials:slc-restart")
      };
      _ = _client.Rest.CreateGuildCommand(restart.Build(), Th3Essentials.Config.GuildId);

      List<SlashCommandOptionBuilder> channelOptions = new List<SlashCommandOptionBuilder>()
          {
            new SlashCommandOptionBuilder()
            {
              Name = "channel",
              Description = Lang.Get("th3essentials:slc-setchannel"),
              Type = ApplicationCommandOptionType.Channel,
              Required = true
            }
          };
      SlashCommandBuilder setchannel = new SlashCommandBuilder
      {
        Name = SlashCommands.SetChannel.ToString().ToLower(),
        Description = Lang.Get("th3essentials:slc-setchannel"),
        Options = channelOptions
      };
      _ = _client.Rest.CreateGuildCommand(setchannel.Build(), Th3Essentials.Config.GuildId);

      List<SlashCommandOptionBuilder> whitelistOptions = new List<SlashCommandOptionBuilder>()
                {
                    new SlashCommandOptionBuilder()
                    {
                        Name = "playername",
                        Description = Lang.Get("th3essentials:slc-whitelist-playername"),
                        Type = ApplicationCommandOptionType.String,
                        Required = true
                    } ,
                    new SlashCommandOptionBuilder()
                    {
                        Name = "mode",
                        Description = Lang.Get("th3essentials:slc-whitelist-mode"),
                        Type = ApplicationCommandOptionType.Boolean,
                        Required = true
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
      _ = _client.Rest.CreateGuildCommand(whitelist.Build(), Th3Essentials.Config.GuildId);

      List<SlashCommandOptionBuilder> charSelectOptions = new List<SlashCommandOptionBuilder>()
        {
          new SlashCommandOptionBuilder()
          {
            Name = "playername",
            Description = Lang.Get("th3essentials:slc-allowcharselonce-playername"),
            Type = ApplicationCommandOptionType.String,
            Required = true
          }
        };
      SlashCommandBuilder allowcharselonce = new SlashCommandBuilder
      {
        Name = SlashCommands.AllowCharSelOnce.ToString().ToLower(),
        Description = Lang.Get("th3essentials:slc-allowcharselonce"),
        Options = charSelectOptions
      };
      _ = _client.Rest.CreateGuildCommand(allowcharselonce.Build(), Th3Essentials.Config.GuildId);
    }

    internal static void HandleSlashCommand(Th3Discord discord, SocketSlashCommand commandInteraction)
    {
      string response;
      if (Enum.TryParse(commandInteraction.Data.Name, true, out SlashCommands cmd))
      {
        switch (cmd)
        {
          case SlashCommands.Players:
            {
              List<string> names = new List<string>();
              foreach (IServerPlayer player in discord._api.World.AllOnlinePlayers)
              {
                names.Add(player.PlayerName);
              }
              response = names.Count == 0 ? Lang.Get("th3essentials:slc-players-none") : string.Join("\n", names);
              break;
            }
          case SlashCommands.Date:
            {
              response = discord._api.World.Calendar.PrettyDate();
              break;
            }
          case SlashCommands.Restart:
            {
              if (discord._config.ShutdownTime != null)
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
                    discord._config.ChannelId = channel.Id;
                    if (!discord.GetDiscordChannel())
                    {
                      discord._api.Server.LogError($"Could not find channel with id: {discord._config.ChannelId}");
                      response = $"Could not find channel with id: {discord._config.ChannelId}";
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
                if (guildUser.GuildPermissions.Administrator)
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
                          discord._api.Logger.VerboseDebug("Something went wrong getting slc-whitelist option");
                          break;
                        }
                    }
                  }

                  IServerPlayerData player = discord._api.PlayerData.GetPlayerDataByLastKnownName(targetPlayer);
                  if (targetPlayer != null && mode != null && player != null)
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
                      ((ServerMain)discord._api.World).PlayerDataManager.WhitelistPlayer(targetPlayer, player.PlayerUID, name, reason, datetime);
                      response = $"Player is now whitelisted until {datetime}";
                    }
                    else
                    {
                      _ = ((ServerMain)discord._api.World).PlayerDataManager.UnWhitelistPlayer(targetPlayer, player.PlayerUID);
                      response = "Player is now removed from whitelist";
                    }
                  }
                  else
                  {
                    response = "Could not find player";
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
          case SlashCommands.AllowCharSelOnce:
            {
              if (commandInteraction.User is SocketGuildUser guildUser)
              {
                if (guildUser.GuildPermissions.Administrator)
                {
                  SocketSlashCommandDataOption option = commandInteraction.Data.Options.First();
                  if (option.Value is string playername)
                  {
                    IServerPlayerData player = discord._api.PlayerData.GetPlayerDataByLastKnownName(playername);
                    if (player != null)
                    {
                      IPlayer playerWoldData = discord._api.World.PlayerByUid(player.PlayerUID);
                      if (playerWoldData != null && SerializerUtil.Deserialize<bool>(playerWoldData.WorldData.GetModdata("createCharacter"), false))
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
                  response = "You need to have Administrator permissions to do that";
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
                  ulong roleid;
                  string mode;
                  foreach (SocketSlashCommandDataOption option in commandInteraction.Data.Options)
                  {
                    switch (option.Name)
                    {
                      case "role":
                        {
                          roleid = (option.Value as SocketRole).Id;
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
                  response = "Error: Channel needs to be a Text Channel";
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
      _ = commandInteraction.RespondAsync(discord.ServerMsg(response), ephemeral: discord._config.UseEphermalCmdResponse);
    }
  }
}