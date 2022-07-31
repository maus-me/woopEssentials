using System;
using System.Collections.Generic;
using Th3Essentials.Config;
using Th3Essentials.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    internal class Warp : Command
    {
        private Th3PlayerConfig _playerConfig;

        internal override void Init(ICoreServerAPI sapi)
        {
            if (Th3Essentials.Config.WarpEnabled)
            {
                _playerConfig = Th3Essentials.PlayerConfig;

                _ = sapi.RegisterCommand("warp", Lang.Get("th3essentials:cd-warp"), "[add|remove|list|&ltwarp name&gt]", OnWarp, Privilege.chat);
            }
        }

        private void OnWarp(IServerPlayer player, int groupId, CmdArgs args)
        {
            string cmd = args.PopWord(string.Empty);
            switch (cmd)
            {
                case "add":
                    {
                        if (!player.HasPrivilege(Privilege.controlserver))
                        {
                            break;
                        }

                        string warpName = args.PopAll();


                        if (warpName == string.Empty)
                        {
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:wp-no-name"), EnumChatType.CommandError);
                            break;
                        }

                        if (Th3Essentials.Config.FindWarpByName(warpName) != null)
                        {
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:wp-exists", warpName), EnumChatType.CommandError);
                            break;
                        }

                        if (Th3Essentials.Config.WarpLocations == null)
                        {
                            Th3Essentials.Config.WarpLocations = new List<HomePoint>();
                        }

                        Th3Essentials.Config.WarpLocations.Add(new HomePoint(warpName, player.Entity.Pos.AsBlockPos));
                        Th3Essentials.Config.MarkDirty();
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:wp-added", warpName), EnumChatType.CommandSuccess);

                        break;
                    }
                case "remove":
                    {
                        if (player.HasPrivilege(Privilege.controlserver))
                        {
                            string warpName = args.PopAll();
                            if (Th3Essentials.Config.WarpLocations != null)
                            {
                                HomePoint warpPoint = Th3Essentials.Config.FindWarpByName(warpName);
                                Th3Essentials.Config.WarpLocations.Remove(warpPoint);
                                Th3Essentials.Config.MarkDirty();
                            }
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:wp-removed", warpName), EnumChatType.CommandSuccess);
                        }
                        break;
                    }
                case "list":
                    {
                        string response = Lang.Get("th3essentials:wp-list") + "\n";

                        if (Th3Essentials.Config.WarpLocations != null)
                        {
                            foreach (HomePoint warpPoint in Th3Essentials.Config.WarpLocations)
                            {
                                response += warpPoint.Name + "\n";
                            }
                        }
                        player.SendMessage(GlobalConstants.GeneralChatGroup, response, EnumChatType.CommandSuccess);
                        break;
                    }
                default:
                    {
                        string warpName = args.PopAll();
                        if (warpName != string.Empty)
                        {
                            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
                            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || Homesystem.CanTravel(playerData))
                            {
                                HomePoint warpPoint = Th3Essentials.Config.FindWarpByName(warpName);
                                if (warpPoint != null)
                                {
                                    Homesystem.TeleportTo(player, playerData, warpPoint.Position);
                                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:wp-to", warpName), EnumChatType.CommandSuccess);
                                }
                                else
                                {
                                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:wp-notfound", warpName), EnumChatType.CommandSuccess);
                                }
                            }
                            else
                            {
                                TimeSpan diff = playerData.HomeLastuseage.AddSeconds(Th3Essentials.Config.HomeCooldown) - DateTime.Now;
                                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.CommandSuccess);
                            }
                        }
                        else
                        {
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:wp-notfound", ""), EnumChatType.CommandError);
                        }
                        break;
                    }
            }
        }
    }
}