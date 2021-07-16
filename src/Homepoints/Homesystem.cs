using System;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Th3Essentials.Config;
using Th3Essentials.PlayerData;

namespace Th3Essentials.Homepoints
{
    internal class Homesystem
    {
        public Th3Config config;

        public Th3PlayerConfig playerConfig;

        public ICoreServerAPI api;

        internal void Init(ICoreServerAPI api)
        {
            this.api = api;
            config = Th3Essentials.Config;
            playerConfig = Th3Essentials.PlayerConfig;
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            api.RegisterCommand("sethome", Lang.Get("th3essentials:cd-sethome"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    AddHome(player, args.PopAll());
                }, Privilege.chat);

            api.RegisterCommand("home", Lang.Get("th3essentials:cd-home"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    FindHome(player, args.PopAll());
                }, Privilege.chat);

            api.RegisterCommand("delhome", Lang.Get("th3essentials:cd-delhome"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    DelHome(player, args.PopAll());
                }, Privilege.chat);

            api.RegisterCommand("spawn", Lang.Get("th3essentials:cd-spawn"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    ToSpawn(player);
                }, Privilege.chat);
        }

        public void ToSpawn(IServerPlayer player)
        {
            Th3PlayerData playerData = playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
                {
                    player.Entity.TeleportTo(api.World.DefaultSpawnPosition);
                    playerData.homeLastuseage = DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-tp-spawn"), EnumChatType.Notification);
                }
                else
                {
                    TimeSpan diff = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown) - DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.Notification);
                }
            }
        }

        public void FindHome(IServerPlayer player, string name) //home Befehl
        {
            Th3PlayerData playerData = playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (name == null || name == string.Empty)
                {
                    if (playerData.homePoints.Count == 0)
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-none"), EnumChatType.Notification);
                        return;
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-list"), EnumChatType.Notification);
                        for (int i = 0; i < playerData.homePoints.Count; i++)
                        {
                            player.SendMessage(GlobalConstants.GeneralChatGroup, playerData.homePoints[i].name, EnumChatType.Notification);

                        }
                    }
                }
                else
                {
                    HomePoint point = playerData.FindPointByName(name);
                    if (point != null)
                    {
                        if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
                        {
                            player.Entity.TeleportTo(point.position);
                            playerData.homeLastuseage = DateTime.Now;
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-tp-point", name), EnumChatType.Notification);
                        }
                        else
                        {
                            TimeSpan diff = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown) - DateTime.Now;
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-404"), EnumChatType.Notification);
                    }
                }
            }
            else
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-none"), EnumChatType.Notification);
            }
        }

        public void DelHome(IServerPlayer player, string name) //delhome Befehl
        {
            Th3PlayerData playerData = playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                HomePoint point = playerData.FindPointByName(name);
                if (point != null)
                {
                    playerData.homePoints.Remove(point);
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-delete", name), EnumChatType.Notification);
                    return;
                }
            }
            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-404"), EnumChatType.Notification);
        }

        public void AddHome(IServerPlayer player, string name) //sethome Befehl
        {
            if (name == string.Empty || name == " " || name == null)
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-empty"), EnumChatType.Notification);
                return;
            }

            Th3PlayerData playerData = playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (playerData.HasMaxHomes())
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-max"), EnumChatType.Notification);
                }
                else
                {
                    if (playerData.FindPointByName(name) == null)
                    {
                        AddNewHomepoint(player, name, playerData);
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-exists"), EnumChatType.Notification);
                    }
                }
            }
            else
            {
                playerData = new Th3PlayerData(player.PlayerUID);
                playerConfig.players.Add(playerData);
                AddNewHomepoint(player, name, playerData);
            }
        }

        private static void AddNewHomepoint(IServerPlayer player, string name, Th3PlayerData playerData)
        {
            HomePoint newPoint = new HomePoint(name, player.Entity.Pos.XYZ.AsBlockPos);
            playerData.homePoints.Add(newPoint);
            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-created", name), EnumChatType.Notification);
        }

        public bool CanTravel(Th3PlayerData playerData)
        {
            DateTime canTravel = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown);
            return canTravel <= DateTime.Now;
        }
    }
}
