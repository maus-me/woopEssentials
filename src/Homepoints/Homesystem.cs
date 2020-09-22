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
            _api.RegisterCommand("sethome", "setzt einen Homepoint auf deine aktuelle Position", "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    Thread adder = new Thread(() => AddHome(player, args.PopAll()));
                    adder.Start();
                }, Privilege.chat);

            _api.RegisterCommand("home", "teleportiert dich zu deinem Homepoint, für Übersicht /home ohne Name verwenden.", "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    Thread searcher = new Thread(() => FindHome(player, args.PopAll()));
                    searcher.Start();
                }, Privilege.chat);

            _api.RegisterCommand("delhome", "löscht einen Homepoint", "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    Thread deleter = new Thread(() => DelHome(player, args.PopAll()));
                    deleter.Start();
                }, Privilege.chat);

            _api.RegisterCommand("spawn", "teleportiert dich zum Spawnpunkt", "",
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
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Teleportiert zum Spawn", EnumChatType.Notification);
                }
                else
                {
                    TimeSpan diff = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown) - DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, $"Du musst noch {diff.Minutes} min {diff.Seconds} sec warten", EnumChatType.Notification);
                }
            }
        }

        public void FindHome(IServerPlayer player, string name) //home Befehl
        {
            CBSPlayerData playerData = playerConfig.getPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (name == null || name == "")
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Deine Homepoints: ", EnumChatType.Notification);
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
                            player.SendMessage(GlobalConstants.GeneralChatGroup, "Teleportiert zu " + name, EnumChatType.Notification);
                        }
                        else
                        {
                            TimeSpan diff = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown) - DateTime.Now;
                            player.SendMessage(GlobalConstants.GeneralChatGroup, $"Du musst noch {diff.Minutes} min {diff.Seconds} sec warten", EnumChatType.Notification);
                        }
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "Homepoint nicht gefunden. Erstelle einen mit /sethome [Name]", EnumChatType.Notification);
                    }
                }
            }
            else
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, "Du hast noch keine Homepoints. Erstelle einen mit /sethome [Name]", EnumChatType.Notification);
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
                    player.SendMessage(GlobalConstants.GeneralChatGroup, name + " gelöscht.", EnumChatType.Notification);
                    return;
                }
            }
            player.SendMessage(GlobalConstants.GeneralChatGroup, "Homepoint nicht gefunden.", EnumChatType.Notification);
        }

        public void AddHome(IServerPlayer player, string name) //sethome Befehl
        {
            if (name == "" || name == " " || name == null)
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, "Name darf nicht leer sein.", EnumChatType.Notification);
                return;
            }

            CBSPlayerData playerData = playerConfig.getPlayerDataByUID(player.PlayerUID);
            if (playerData != null)
            {
                if (playerData.hasMaxHomes())
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Maximale Anzahl an Homepoints erreicht.", EnumChatType.Notification);
                }
                else
                {
                    if (playerData.findPointByName(name) == null)
                    {
                        addNewHomepoint(player, name, playerData);
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "Homepoint mit diesem Namen existiert bereits.", EnumChatType.Notification);
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
            HomePoint newPoint = new HomePoint(name, player.Entity.Pos.XYZ);
            playerData.homePoints.Add(newPoint);
            player.SendMessage(GlobalConstants.GeneralChatGroup, "Homepoint mit dem Namen " + name + " wurde erstellt.", EnumChatType.Notification);
        }

        public bool canTravel(CBSPlayerData playerData)
        {
            DateTime canTravel = playerData.homeLastuseage.AddMinutes(playerData.homeCooldown);
            return canTravel <= DateTime.Now;
        }
    }
}
