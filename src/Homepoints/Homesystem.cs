using System;
using Th3Essentials.Config;
using Th3Essentials.PlayerData;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Homepoints
{
    internal class Homesystem
    {
        private Th3PlayerConfig _playerConfig;

        private ICoreServerAPI _api;

        internal void Init(ICoreServerAPI api)
        {
            _api = api;
            _playerConfig = Th3Essentials.PlayerConfig;
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            _api.RegisterCommand("sethome", Lang.Get("th3essentials:cd-sethome"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    AddHome(player, args.PopAll());
                }, Privilege.chat);

            _api.RegisterCommand("home", Lang.Get("th3essentials:cd-home"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    FindHome(player, args.PopAll());
                }, Privilege.chat);

            _api.RegisterCommand("delhome", Lang.Get("th3essentials:cd-delhome"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    DelHome(player, args.PopAll());
                }, Privilege.chat);

            _api.RegisterCommand("spawn", Lang.Get("th3essentials:cd-spawn"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    ToSpawn(player);
                }, Privilege.chat);
        }

        public void ToSpawn(IServerPlayer player)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
                {
                    player.Entity.TeleportTo(_api.World.DefaultSpawnPosition);
                    playerData.HomeLastuseage = DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-tp-spawn"), EnumChatType.Notification);
                }
                else
                {
                    TimeSpan diff = playerData.HomeLastuseage.AddMinutes(playerData.HomeCooldown) - DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.Notification);
                }
            }
        }

        public void FindHome(IServerPlayer player, string name) //home Befehl
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (name == null || name == string.Empty)
                {
                    if (playerData.HomePoints.Count == 0)
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-none"), EnumChatType.Notification);
                        return;
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-list"), EnumChatType.Notification);
                        for (int i = 0; i < playerData.HomePoints.Count; i++)
                        {
                            player.SendMessage(GlobalConstants.GeneralChatGroup, playerData.HomePoints[i].Name, EnumChatType.Notification);

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
                            player.Entity.TeleportTo(point.Position);
                            playerData.HomeLastuseage = DateTime.Now;
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-tp-point", name), EnumChatType.Notification);
                        }
                        else
                        {
                            TimeSpan diff = playerData.HomeLastuseage.AddMinutes(playerData.HomeCooldown) - DateTime.Now;
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
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                HomePoint point = playerData.FindPointByName(name);
                if (point != null)
                {
                    playerData.HomePoints.Remove(point);
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

            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
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
                _playerConfig.Players.Add(playerData);
                AddNewHomepoint(player, name, playerData);
            }
        }

        private static void AddNewHomepoint(IServerPlayer player, string name, Th3PlayerData playerData)
        {
            HomePoint newPoint = new HomePoint(name, player.Entity.Pos.XYZ.AsBlockPos);
            playerData.HomePoints.Add(newPoint);
            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-created", name), EnumChatType.Notification);
        }

        public bool CanTravel(Th3PlayerData playerData)
        {
            DateTime canTravel = playerData.HomeLastuseage.AddMinutes(playerData.HomeCooldown);
            return canTravel <= DateTime.Now;
        }
    }
}
