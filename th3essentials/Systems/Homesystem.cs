using System;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Th3Essentials.Systems
{
    internal class Homesystem
    {
        private Th3PlayerConfig _playerConfig;

        private Th3Config _config;

        private ICoreServerAPI _sapi;

        internal void Init(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            _playerConfig = Th3Essentials.PlayerConfig;
            _config = Th3Essentials.Config;
            if (_config.HomeLimit > 0)
            {
                _ = _sapi.RegisterCommand("sethome", Lang.Get("th3essentials:cd-sethome"), "[Name]",
                    (player, groupId, args) =>
                    {
                        SetHome(player, args.PopAll());
                    }, Privilege.chat);

                _ = _sapi.RegisterCommand("home", Lang.Get("th3essentials:cd-home"), "[Name]",
                    (player, groupId, args) =>
                    {
                        Home(player, args.PopAll());
                    }, Privilege.chat);

                _ = _sapi.RegisterCommand("delhome", Lang.Get("th3essentials:cd-delhome"), "[Name]",
                    (player, groupId, args) =>
                    {
                        DeleteHome(player, args.PopAll());
                    }, Privilege.chat);
            }
            if (_config.SpawnEnabled)
            {
                _ = _sapi.RegisterCommand("spawn", Lang.Get("th3essentials:cd-spawn"), string.Empty,
                    (player, groupId, args) =>
                    {
                        ToSpawn(player);
                    }, Privilege.chat);
            }
            if (_config.BackEnabled)
            {
                _sapi.Event.PlayerDeath += PlayerDied;
                _ = _sapi.RegisterCommand("back", Lang.Get("th3essentials:cd-back"), string.Empty,
                (player, groupId, args) =>
                {
                    TeleportBack(player);
                }, Privilege.chat);
            }
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
                TimeSpan diff = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown) - DateTime.Now;
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.CommandSuccess);
            }
        }

        public void ToSpawn(IServerPlayer player)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
            {
                playerData.LastPosition = player.Entity.Pos.AsBlockPos;
                player.Entity.TeleportTo(_sapi.World.DefaultSpawnPosition);
                playerData.HomeLastuseage = DateTime.Now;
                playerData.MarkDirty();
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-tp-spawn"), EnumChatType.CommandSuccess);
            }
            else
            {
                TimeSpan diff = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown) - DateTime.Now;
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
                    string response = Lang.Get("th3essentials:hs-list", $"{playerData.HomePoints.Count}/{_config.HomeLimit}\n");
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
                        TimeSpan diff = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown) - DateTime.Now;
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds), EnumChatType.CommandSuccess);
                    }
                }
                else
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-404"), EnumChatType.CommandSuccess);
                }
            }
        }

        public void DeleteHome(IServerPlayer player, string name)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            HomePoint point = playerData.FindPointByName(name);
            if (point != null)
            {
                _ = playerData.HomePoints.Remove(point);
                playerData.MarkDirty();
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-delete", name), EnumChatType.CommandSuccess);
                return;
            }
            else
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-404"), EnumChatType.CommandSuccess);
            }
        }

        public void SetHome(IServerPlayer player, string name)
        {
            if (name == string.Empty || name == " " || name == null)
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:hs-empty"), EnumChatType.CommandSuccess);
                return;
            }

            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData.HomePoints.Count >= _config.HomeLimit)
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
            DateTime canTravel = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown);
            return canTravel <= DateTime.Now;
        }
    }
}
