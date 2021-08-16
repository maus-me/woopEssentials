using System;
using Th3Essentials.Config;
using Th3Essentials.PlayerData;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
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
            _api.Event.PlayerDeath += PlayerDied;
        }

        private void RegisterCommands()
        {
            _api.RegisterCommand("sethome", Lang.Get("th3essentials:cd-sethome"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    SetHome(player, args.PopAll());
                }, Privilege.chat);

            _api.RegisterCommand("home", Lang.Get("th3essentials:cd-home"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    Home(player, args.PopAll());
                }, Privilege.chat);

            _api.RegisterCommand("delhome", Lang.Get("th3essentials:cd-delhome"), "[Name]",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    DeleteHome(player, args.PopAll());
                }, Privilege.chat);

            _api.RegisterCommand("spawn", Lang.Get("th3essentials:cd-spawn"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    ToSpawn(player);
                }, Privilege.chat);
            _api.RegisterCommand("back", Lang.Get("th3essentials:cd-back"), string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                TeleportBack(player);
            }, Privilege.chat);
        }

        private void PlayerDied(IServerPlayer byPlayer, DamageSource damageSource)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(byPlayer.PlayerUID);
            playerData.LastPosition = byPlayer.Entity.Pos.AsBlockPos;
            playerData.MarkDirty();
        }

        private void TeleportBack(IServerPlayer player)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
            {
                if (playerData.LastPosition == null)
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-noBack"), EnumChatType.CommandError);
                }
                else
                {
                    BlockPos teleportTo = playerData.LastPosition;
                    playerData.LastPosition = player.Entity.Pos.AsBlockPos;
                    player.Entity.TeleportTo(teleportTo);
                    playerData.HomeLastuseage = DateTime.Now;
                    playerData.MarkDirty();
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-back"), EnumChatType.CommandSuccess);
                }
            }
            else
            {
                TimeSpan diff = playerData.HomeLastuseage.AddMinutes(playerData.HomeCooldown) - DateTime.Now;
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.CommandSuccess);
            }
        }

        public void ToSpawn(IServerPlayer player)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
            {
                playerData.LastPosition = player.Entity.Pos.AsBlockPos;
                player.Entity.TeleportTo(_api.World.DefaultSpawnPosition);
                playerData.HomeLastuseage = DateTime.Now;
                playerData.MarkDirty();
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-tp-spawn"), EnumChatType.CommandSuccess);
            }
            else
            {
                TimeSpan diff = playerData.HomeLastuseage.AddMinutes(playerData.HomeCooldown) - DateTime.Now;
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.CommandSuccess);
            }

        }

        public void Home(IServerPlayer player, string name)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (name == null || name == string.Empty)
            {
                if (playerData.HomePoints.Count == 0)
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-none"), EnumChatType.CommandSuccess);
                    return;
                }
                else
                {
                    string response = Lang.Get("th3essentials:hs-list");
                    for (int i = 0; i < playerData.HomePoints.Count; i++)
                    {
                        response += playerData.HomePoints[i].Name + "\n";
                    }
                    player.SendMessage(GlobalConstants.GeneralChatGroup, response, EnumChatType.CommandSuccess);
                }
            }
            else
            {
                HomePoint point = playerData.FindPointByName(name);
                if (point != null)
                {
                    if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
                    {
                        playerData.LastPosition = player.Entity.Pos.AsBlockPos;
                        player.Entity.TeleportTo(point.Position);
                        playerData.HomeLastuseage = DateTime.Now;
                        playerData.MarkDirty();
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-tp-point", name), EnumChatType.CommandSuccess);
                    }
                    else
                    {
                        TimeSpan diff = playerData.HomeLastuseage.AddMinutes(playerData.HomeCooldown) - DateTime.Now;
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.CommandSuccess);
                    }
                }
                else
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-404"), EnumChatType.CommandSuccess);
                }
            }
        }

        public void DeleteHome(IServerPlayer player, string name) //delhome Befehl
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            HomePoint point = playerData.FindPointByName(name);
            if (point != null)
            {
                playerData.HomePoints.Remove(point);
                playerData.MarkDirty();
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-delete", name), EnumChatType.CommandSuccess);
                return;
            }
            else
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-404"), EnumChatType.CommandSuccess);
            }
        }

        public void SetHome(IServerPlayer player, string name) //sethome Befehl
        {
            if (name == string.Empty || name == " " || name == null)
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-empty"), EnumChatType.CommandSuccess);
                return;
            }

            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData.HasMaxHomes())
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-max"), EnumChatType.CommandSuccess);
            }
            else
            {
                if (playerData.FindPointByName(name) == null)
                {
                    HomePoint newPoint = new HomePoint(name, player.Entity.Pos.XYZ.AsBlockPos);
                    playerData.HomePoints.Add(newPoint);
                    playerData.MarkDirty();
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-created", name), EnumChatType.CommandSuccess);
                }
                else
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-exists"), EnumChatType.CommandSuccess);
                }
            }
        }

        public bool CanTravel(Th3PlayerData playerData)
        {
            DateTime canTravel = playerData.HomeLastuseage.AddMinutes(playerData.HomeCooldown);
            return canTravel <= DateTime.Now;
        }
    }
}
