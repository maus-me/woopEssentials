using System;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using System.IO;
using System.Threading;

namespace CBSEssentials.Homepoints
{
    internal class Homesystem
    {
        public Homes homes;

        private const string configFile = "homesystem.json";

        public ICoreServerAPI _api;

        internal void init(ICoreServerAPI api)
        {
            _api = api;
            _api.Event.GameWorldSave += GameWorldSave;

            homes = _api.LoadModConfig<Homes>(configFile);

            if (homes == null)
            {
                homes = new Homes();
                _api.StoreModConfig(homes, configFile);
                _api.Server.LogWarning("Homesystem initialized with default config!!!");
                _api.Server.LogWarning("Homesystem config file at " + Path.Combine(GamePaths.ModConfig, configFile));
            }

            registerCommands();
        }

        private void GameWorldSave()
        {
            _api.StoreModConfig(homes, configFile);
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
            PlayerHomes playerhome = homes.findPlayerhomeByUID(player.PlayerUID);
            if (playerhome != null)
            {
                if (canTravel(playerhome.lastUse))
                {
                    player.Entity.TeleportTo(_api.World.DefaultSpawnPosition);
                    playerhome.lastUse = DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Teleportiert zum Spawn", EnumChatType.Notification);
                }
                else
                {
                    TimeSpan diff = playerhome.lastUse.AddMinutes(homes.cooldown) - DateTime.Now;
                    player.SendMessage(GlobalConstants.GeneralChatGroup, $"Du musst noch {diff.Minutes} min {diff.Seconds} sec warten", EnumChatType.Notification);
                }
            }
        }

        public void FindHome(IServerPlayer player, string name) //home Befehl
        {
            PlayerHomes playerhome = homes.findPlayerhomeByUID(player.PlayerUID);
            if (playerhome != null)
            {
                if (name == null || name == "")
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Deine Homepoints: ", EnumChatType.Notification);
                    for (int i = 0; i < playerhome.points.Count; i++)
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, playerhome.points[i].name, EnumChatType.Notification);

                    }
                }
                else
                {
                    Point point = playerhome.findPointByName(name);
                    if (point != null)
                    {
                        if (canTravel(playerhome.lastUse))
                        {
                            player.Entity.TeleportTo(point.position);
                            playerhome.lastUse = DateTime.Now;
                            player.SendMessage(GlobalConstants.GeneralChatGroup, "Teleportiert zu " + name, EnumChatType.Notification);
                        }
                        else
                        {
                            TimeSpan diff = playerhome.lastUse.AddMinutes(homes.cooldown) - DateTime.Now;
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
            PlayerHomes playerhome = homes.findPlayerhomeByUID(player.PlayerUID);
            if (playerhome != null)
            {
                Point point = playerhome.findPointByName(name);
                if (point != null)
                {
                    playerhome.points.Remove(point);
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

            PlayerHomes playerhome = homes.findPlayerhomeByUID(player.PlayerUID);
            if (playerhome != null)
            {
                if (playerhome.hasMaxHomes(homes.maxhomes))
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Maximale Anzahl an Homepoints erreicht.", EnumChatType.Notification);
                }
                else
                {
                    if (playerhome.findPointByName(name) == null)
                    {
                        addNewHomepoint(player, name, playerhome);
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "Homepoint mit diesem Namen existiert bereits.", EnumChatType.Notification);
                    }
                }
            }
            else
            {
                playerhome = new PlayerHomes(player.PlayerUID, player.PlayerName);
                homes.playerhomes.Add(playerhome);
                addNewHomepoint(player, name, playerhome);
            }
        }

        private static void addNewHomepoint(IServerPlayer player, string name, PlayerHomes playerhome)
        {
            Point newPoint = new Point(name, player.Entity.Pos.XYZ);
            playerhome.points.Add(newPoint);
            player.SendMessage(GlobalConstants.GeneralChatGroup, "Homepoint mit dem Namen " + name + " wurde erstellt.", EnumChatType.Notification);
        }

        public bool canTravel(DateTime lastTravel)
        {
            DateTime canTravel = lastTravel.AddMinutes(homes.cooldown);
            return canTravel <= DateTime.Now;
        }
    }
}
