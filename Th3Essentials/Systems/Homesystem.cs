using System;
using System.Linq;
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
                _sapi.ChatCommands.Create("sethome")
                    .WithDescription(Lang.Get("th3essentials:cd-sethome"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
                    .HandleWith(SetHome);

                _sapi.ChatCommands.Create("home")
                    .WithDescription(Lang.Get("th3essentials:cd-home"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .WithArgs(_sapi.ChatCommands.Parsers.OptionalWord("name"))
                    .HandleWith(Home);

                _sapi.ChatCommands.Create("delhome")
                    .WithDescription(Lang.Get("th3essentials:cd-delhome"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
                    .HandleWith(DeleteHome);
            }

            if (_config.SpawnEnabled)
            {
                _sapi.ChatCommands.Create("spawn")
                    .WithDescription(Lang.Get("th3essentials:cd-spawn"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .HandleWith(ToSpawn);
            }

            if (_config.BackEnabled)
            {
                _sapi.ChatCommands.Create("back")
                    .WithDescription(Lang.Get("th3essentials:cd-back"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .HandleWith(TeleportBack);
                _sapi.Event.PlayerDeath += PlayerDied;
            }
        }

        private void PlayerDied(IServerPlayer byPlayer, DamageSource damageSource)
        {
            var playerData = _playerConfig.GetPlayerDataByUID(byPlayer.PlayerUID);
            playerData.LastPosition = byPlayer.Entity.Pos.AsBlockPos;
            playerData.MarkDirty();
        }

        public TextCommandResult TeleportBack(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
            {
                if (playerData.LastPosition == null)
                {
                    return TextCommandResult.Error(Lang.Get("th3essentials:hs-noBack"));
                }

                TeleportTo(player, playerData, playerData.LastPosition);
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-back"));
            }

            var diff = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown) - DateTime.Now;
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds));
        }

        public TextCommandResult ToSpawn(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
            {
                TeleportTo(player, playerData, _sapi.World.DefaultSpawnPosition.AsBlockPos);
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-tp-spawn"));
            }

            var diff = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown) - DateTime.Now;
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds));
        }

        public TextCommandResult Home(TextCommandCallingArgs args)
        {
            var name = args.Parsers[0].GetValue() as string;
            var player = args.Caller.Player;
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            var response = Lang.Get("th3essentials:hs-list", $"{playerData.HomePoints.Count}/{_config.HomeLimit}\n");
            if (string.IsNullOrEmpty(name))
            {
                if (playerData.HomePoints.Count == 0)
                {
                    return TextCommandResult.Success(Lang.Get("th3essentials:hs-none"));
                }

                response = playerData.HomePoints.Aggregate(response, (current, t) => current + (t.Name + "\n"));

                return TextCommandResult.Success(response);
            }

            var point = playerData.FindPointByName(name);
            if (point == null) return TextCommandResult.Success(Lang.Get("th3essentials:hs-404"));

            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
            {
                TeleportTo(player, playerData, point.Position);
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-tp-point", name));
            }

            var diff = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown) - DateTime.Now;
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds));
        }

        public TextCommandResult DeleteHome(TextCommandCallingArgs args)
        {
            var name = args.Parsers[0].GetValue() as string;
            var player = args.Caller.Player;
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            var point = playerData.FindPointByName(name);

            if (point == null) return TextCommandResult.Success(Lang.Get("th3essentials:hs-404"));

            _ = playerData.HomePoints.Remove(point);
            playerData.MarkDirty();
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-delete", name));
        }

        public TextCommandResult SetHome(TextCommandCallingArgs args)
        {
            var name = args.Parsers[0].GetValue() as string;
            var player = args.Caller.Player;
            if (string.IsNullOrWhiteSpace(name))
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-empty"));
            }

            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData.HomePoints.Count >= _config.HomeLimit)
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-max"));
            }

            if (playerData.FindPointByName(name) == null)
            {
                var newPoint = new HomePoint(name, player.Entity.Pos.XYZ.AsBlockPos);
                playerData.HomePoints.Add(newPoint);
                playerData.MarkDirty();
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-created", name));
            }

            return TextCommandResult.Success(Lang.Get("th3essentials:hs-exists"));
        }

        public static void TeleportTo(IPlayer player, Th3PlayerData playerData, BlockPos location)
        {
            playerData.LastPosition = player.Entity.Pos.AsBlockPos;
            player.Entity.TeleportTo(new Vec3d(location.X + 0.5,location.Y + 0.5,location.Z + 0.5));
            playerData.HomeLastuseage = DateTime.Now;
            playerData.MarkDirty();
        }

        public static bool CanTravel(Th3PlayerData playerData)
        {
            var canTravel = playerData.HomeLastuseage.AddSeconds(Th3Essentials.Config.HomeCooldown);
            return canTravel <= DateTime.Now;
        }
    }
}