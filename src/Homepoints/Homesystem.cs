using System;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.Threading;
using CBSEssentials.Config;
using CBSEssentials.PlayerData;

namespace CBSEssentials.Homepoints
{
    internal class Homesystem
    {
        public CBSConfig config;
        public CBSPlayerConfig playerConfig;

        public ICoreServerAPI _api;

        internal void init(ICoreServerAPI api)
        {
            _api = api;
            this.config = CBSEssentials.config;
            this.playerConfig = CBSEssentials.playerConfig;

            registerCommands();
        }

        private void registerCommands()
        {
            _api.RegisterCommand("sethome", Lang.Get("cbsessentials:cd-sethome"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    Thread adder = new Thread(() => AddHome(player, args.PopAll()));
                    adder.Start();
                }, Privilege.chat);

            _api.RegisterCommand("home", Lang.Get("cbsessentials:cd-home"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    Thread searcher = new Thread(() => FindHome(player, args.PopAll()));
                    searcher.Start();
                }, Privilege.chat);

            _api.RegisterCommand("delhome", Lang.Get("cbsessentials:cd-delhome"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    Thread deleter = new Thread(() => DelHome(player, args.PopAll()));
                    deleter.Start();
                }, Privilege.chat);

            _api.RegisterCommand("spawn", Lang.Get("cbsessentials:cd-spawn"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    Thread toSpawner = new Thread(() => ToSpawn(player));
                    toSpawner.Start();
                }, Privilege.chat);
        }

        public void ToSpawn(IServerPlayer player)
        {
            CBSPlayerData playerData = playerConfig.getPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (canTravel(playerData))
                {
                    player.Entity.TeleportTo(_api.World.DefaultSpawnPosition);
                    playerData.homeLastuseage = DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-tp-spawn"), EnumChatType.Notification);
                }
                else
                {
                    TimeSpan diff = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown) - DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.Notification);
                }
            }
        }

        public void FindHome(IServerPlayer player, string name) //home Befehl
        {
            CBSPlayerData playerData = playerConfig.getPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (name == null || name == string.Empty)
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-list"), EnumChatType.Notification);
                    for (int i = 0; i < playerData.homePoints.Count; i++)
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, playerData.homePoints[i].name, EnumChatType.Notification);

                    }
                }
                else
                {
                    HomePoint point = playerData.findPointByName(name);
                    if (point != null)
                    {
                        if (canTravel(playerData))
                        {
                            player.Entity.TeleportTo(point.position);
                            playerData.homeLastuseage = DateTime.Now;
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-tp-point", name), EnumChatType.Notification);
                        }
                        else
                        {
                            TimeSpan diff = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown) - DateTime.Now;
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-404"), EnumChatType.Notification);
                    }
                }
            }
            else
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-none"), EnumChatType.Notification);
            }
        }

        public void DelHome(IServerPlayer player, string name) //delhome Befehl
        {
            CBSPlayerData playerData = playerConfig.getPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                HomePoint point = playerData.findPointByName(name);
                if (point != null)
                {
                    playerData.homePoints.Remove(point);
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-delete", name), EnumChatType.Notification);
                    return;
                }
            }
            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-404"), EnumChatType.Notification);
        }

        public void AddHome(IServerPlayer player, string name) //sethome Befehl
        {
            if (name == string.Empty || name == " " || name == null)
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-empty"), EnumChatType.Notification);
                return;
            }

            CBSPlayerData playerData = playerConfig.getPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (playerData.hasMaxHomes())
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-max"), EnumChatType.Notification);
                }
                else
                {
                    if (playerData.findPointByName(name) == null)
                    {
                        addNewHomepoint(player, name, playerData);
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-exists"), EnumChatType.Notification);
                    }
                }
            }
            else
            {
                playerData = new CBSPlayerData(player.PlayerUID);
                playerConfig.players.Add(playerData);
                addNewHomepoint(player, name, playerData);
            }
        }

        private static void addNewHomepoint(IServerPlayer player, string name, CBSPlayerData playerData)
        {
            HomePoint newPoint = new HomePoint(name, player.Entity.Pos.XYZ.AsBlockPos);
            playerData.homePoints.Add(newPoint);
            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:hs-created", name), EnumChatType.Notification);
        }

        public bool canTravel(CBSPlayerData playerData)
        {
            DateTime canTravel = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown);
            return canTravel <= DateTime.Now;
        }
    }
}
