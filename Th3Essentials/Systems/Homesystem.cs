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
                _sapi.ChatCommands.Create("home")
                    .WithDescription(Lang.Get("th3essentials:cd-home"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .HandleWith(Home)
                    
                    .BeginSubCommand("delete")
                        .WithAlias("del", "rm", "d", "r")
                        .WithDescription(Lang.Get("th3essentials:cd-delhome"))
                        .RequiresPlayer()
                        .RequiresPrivilege(Privilege.chat)
                        .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
                        .HandleWith(DeleteHome)
                    .EndSubCommand()
                    
                    .BeginSubCommand("set")
                        .WithAlias("s","new","add")
                        .WithDescription(Lang.Get("th3essentials:cd-sethome"))
                        .RequiresPlayer()
                        .RequiresPrivilege(Privilege.chat)
                        .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
                        .HandleWith(SetHome)
                    .EndSubCommand()
                    
                    .BeginSubCommand("list")
                    .WithAlias("ls","l")
                    .WithDescription(Lang.Get("th3essentials:cd-lshome"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .HandleWith(OnList)
                    .EndSubCommand()
                    
                    .BeginSubCommand("limit")
                        .WithDescription(Lang.Get("th3essentials:cd-limithome"))
                        .RequiresPlayer()
                        .RequiresPrivilege(Privilege.commandplayer)
                        .WithArgs(_sapi.ChatCommands.Parsers.OnlinePlayer("player"),_sapi.ChatCommands.Parsers.Int("limit"))
                        .HandleWith(ChangeLimit)
                    .EndSubCommand()
                    ;
                
                //TODO remove in next version
                _sapi.ChatCommands.Create("sethome")
                    .WithDescription(Lang.Get("th3essentials:cd-sethome") + " Deprecated: use /home set [name] instead")
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
                    .HandleWith(SetHome);

                //TODO remove in next version
                _sapi.ChatCommands.Create("delhome")
                    .WithDescription(Lang.Get("th3essentials:cd-delhome") + " Deprecated: use /home delete [name] instead")
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

        private TextCommandResult ChangeLimit(TextCommandCallingArgs args)
        {
            var player = args.Parsers[0].GetValue() as IPlayer;
            
            if (player == null) return TextCommandResult.Error("Could not get player data");
            
            var limit = (int)args.Parsers[1].GetValue();
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID, false);
            playerData.HomeLimit = limit;
            playerData.MarkDirty();
            
            return TextCommandResult.Success($"Updated home point limit for player : {player.PlayerName} to {limit}");
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
            var name = args.RawArgs.PopWord();
            
            var player = args.Caller.Player;
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (string.IsNullOrEmpty(name))
            {
                if (playerData.HomePoints.Count == 0)
                {
                    return TextCommandResult.Success(Lang.Get("th3essentials:hs-none"));
                }

                return OnList(args);
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

        private TextCommandResult OnList(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            
            var response = Lang.Get("th3essentials:hs-list", $"{playerData.HomePoints.Count}/{GetPlayerHomeLimit(args.Caller.Player)}\n");
            response = playerData.HomePoints.Aggregate(response, (current, t) => current + (t.Name + "\n"));

            return TextCommandResult.Success(response);
        }

        public TextCommandResult DeleteHome(TextCommandCallingArgs args)
        {
            
            //TODO remove in next version
            if (args.Command.Name.Equals("delhome"))
            {
                (args.Caller.Player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, "Deprecated: use /home set [name] instead", EnumChatType.Notification);
            }
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
            //TODO remove in next version
            if (args.Command.Name.Equals("sethome"))
            {
                (args.Caller.Player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, "Deprecated: use /home set [name] instead", EnumChatType.Notification);
            }
            var name = args.Parsers[0].GetValue() as string;
            var player = args.Caller.Player;
            if (string.IsNullOrWhiteSpace(name))
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-empty"));
            }

            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData.HomePoints.Count >= GetPlayerHomeLimit(args.Caller.Player))
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

        public int GetPlayerHomeLimit(IPlayer callerPlayer)
        {
            var playerDataByUid = _playerConfig.GetPlayerDataByUID(callerPlayer.PlayerUID, false);
            return playerDataByUid.HomeLimit >= 0 ? playerDataByUid.HomeLimit : _config.HomeLimit;
        }
    }
}