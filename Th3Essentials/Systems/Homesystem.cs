using System;
using System.Linq;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
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
            if (_config.HomeLimit >= 0)
            {
                _sapi.ChatCommands.Create("home")
                    .WithDescription(Lang.Get("th3essentials:cd-home"))
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .IgnoreAdditionalArgs()
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
                    
                    .BeginSubCommand("item")
                        .RequiresPrivilege(Privilege.controlserver)
                        .RequiresPlayer()
                        .WithDescription(Lang.Get("th3essentials:hs-item-desc"))
                        .HandleWith(SetItem)
                    .EndSubCommand()
                    
                    .BeginSubCommand("setitem")
                        .RequiresPrivilege(Privilege.controlserver)
                        .RequiresPlayer()
                        .WithDescription(Lang.Get("th3essentials:hs-item-desc"))
                        .HandleWith(SetSetItem)
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

        private TextCommandResult SetSetItem(TextCommandCallingArgs args)
        {
            var slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;

            if (slot.Itemstack == null)
            {
                _config.SetHomeItem = null;
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-item-unset"));
            }
        
            var enumItemClass = slot.Itemstack.Class;
            var stackSize = slot.Itemstack.StackSize;
            var code = slot.Itemstack.Collectible.Code;

            if (slot.Itemstack.Attributes is not TreeAttribute attributes) return TextCommandResult.Success("error not a TreeAttribute");
                        
            // remove food perish data
            attributes.RemoveAttribute("transitionstate");

            _config.SetHomeItem = new StarterkitItem(enumItemClass, code, stackSize, attributes);
            _config.MarkDirty();
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-item-set"));
        }

        private TextCommandResult ChangeLimit(TextCommandCallingArgs args)
        {
            var player = args.Parsers[0].GetValue() as IPlayer;
            
            if (player == null) return TextCommandResult.Error("Could not get player data");
            
            var limit = (int)args.Parsers[1].GetValue();
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID, false);
            playerData.HomeLimit = limit;
            playerData.MarkDirty();
            
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-changelim", player.PlayerName, limit));
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
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData, _config.BackCooldown))
            {
                if (playerData.LastPosition == null)
                {
                    return TextCommandResult.Error(Lang.Get("th3essentials:hs-noBack"));
                }
                
                var playerConfig = GetConfig(player, playerData, _config);
                if (CheckPayment(_config.HomeItem, playerConfig.BackTeleportCost, player, out var canTeleport, out var success)) return success!;
                if (canTeleport)
                {
                    PayIfNeeded(player, _config.HomeItem, playerConfig.BackTeleportCost);
                    TeleportTo(player, playerData, playerData.LastPosition, _config.ExcludeBackFromBack);
                    return TextCommandResult.Success(Lang.Get("th3essentials:hs-back"));
                }
                
                return TextCommandResult.Success("Could not teleport");
            }

            var diff = playerData.HomeLastuseage.AddSeconds(_config.BackCooldown) - DateTime.Now;
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds));
        }

        public TextCommandResult ToSpawn(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
            { 
                var playerConfig = GetConfig(player, playerData, _config);
                if (CheckPayment(_config.HomeItem, playerConfig.HomeTeleportCost, player, out var canTeleport, out var success)) return success!;
                if (canTeleport)
                {
                    PayIfNeeded(player, _config.HomeItem, playerConfig.HomeTeleportCost);
                    TeleportTo(player, playerData, _sapi.World.DefaultSpawnPosition.AsBlockPos);
                    return TextCommandResult.Success(Lang.Get("th3essentials:hs-tp-spawn"));
                }
                
                return TextCommandResult.Success("Could not teleport");
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

            var playerConfig = GetConfig(player, playerData, _config);
            
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || CanTravel(playerData))
            {
                if (CheckPayment(_config.HomeItem, playerConfig.HomeTeleportCost, player, out var canTeleport, out var success)) return success!;

                if (canTeleport && player.InventoryManager.ActiveHotbarSlot != null)
                {
                    PayIfNeeded(player, _config.HomeItem, playerConfig.HomeTeleportCost);
                    TeleportTo(player, playerData, point.Position, _config.ExcludeHomeFromBack);
                    return TextCommandResult.Success(Lang.Get("th3essentials:hs-tp-point", name));
                }

                return TextCommandResult.Success("Could not teleport");
            }

            var diff = playerData.HomeLastuseage.AddSeconds(_config.HomeCooldown) - DateTime.Now;
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-wait", diff.Minutes, diff.Seconds));
        }

        internal static void PayIfNeeded(IPlayer player, StarterkitItem? item, int cost)
        {
            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || item == null) return;
            
            player.InventoryManager.ActiveHotbarSlot.TakeOut(cost);
            player.InventoryManager.ActiveHotbarSlot.MarkDirty();
        }

        public static bool CheckPayment(StarterkitItem? item, int cost, IPlayer player, out bool canTeleport, out TextCommandResult? success)
        {
            canTeleport = true;
            if (item != null && cost > 0 &&
                player.WorldData.CurrentGameMode != EnumGameMode.Creative)
            {
                canTeleport = false;
                var itemStack = player.InventoryManager.ActiveHotbarSlot?.Itemstack;
                switch (item.Itemclass)
                {
                    case EnumItemClass.Block:
                    {
                        if (Equals(item.Code, itemStack?.Block?.Code) &&
                            itemStack.StackSize >= cost)
                        {
                            canTeleport = true;
                        }
                        else
                        {
                            var itemName =
                                Lang.Get(item.Itemclass.ToString().ToLowerInvariant() + "-" + item.Code.Path);
                            success = TextCommandResult.Success(
                                Lang.Get("th3essentials:hs-item-missing", cost, itemName));
                            return true;
                        }
                        break;
                    }
                    case EnumItemClass.Item:
                    {
                        if (Equals(item.Code, itemStack?.Item?.Code) &&
                            itemStack.StackSize >= cost)
                        {
                            canTeleport = true;
                        }
                        else
                        {
                            var itemName =
                                Lang.Get(item.Itemclass.ToString().ToLowerInvariant() + "-" + item.Code.Path);
                            success = TextCommandResult.Success(
                                Lang.Get("th3essentials:hs-item-missing", cost, itemName));
                            return true;
                        }
                        break;
                    }
                }
            }

            success = null;
            return false;
        }

        private TextCommandResult OnList(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            
            var response = Lang.Get("th3essentials:hs-list", $"{playerData.HomePoints.Count}/{GetPlayerHomeLimit(args.Caller.Player, playerData)}\n");
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
            if (playerData.HomePoints.Count >= GetPlayerHomeLimit(args.Caller.Player, playerData))
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-max"));
            }

            if (playerData.FindPointByName(name) == null)
            {
                
                var playerConfig = GetConfig(player, playerData, _config);
                if(CheckPayment(_config.SetHomeItem, playerConfig.SetHomeCost, player, out var canTeleport, out var success)) return success!;
                if (canTeleport)
                {
                    PayIfNeeded(player, _config.SetHomeItem, playerConfig.SetHomeCost);
                    var newPoint = new HomePoint(name, player.Entity.Pos.XYZ.AsBlockPos);
                    playerData.HomePoints.Add(newPoint);
                    playerData.MarkDirty();
                    return TextCommandResult.Success(Lang.Get("th3essentials:hs-created", name));
                }
                return TextCommandResult.Error("Something went wrong");
            }

            return TextCommandResult.Success(Lang.Get("th3essentials:hs-exists"));
        }
        private TextCommandResult SetItem(TextCommandCallingArgs args)
        {
            var slot = args.Caller.Player.InventoryManager.ActiveHotbarSlot;

            if (slot.Itemstack == null)
            {
                _config.HomeItem = null;
                return TextCommandResult.Success(Lang.Get("th3essentials:hs-item-unset"));
            }
        
            var enumItemClass = slot.Itemstack.Class;
            var stackSize = slot.Itemstack.StackSize;
            var code = slot.Itemstack.Collectible.Code;

            if (slot.Itemstack.Attributes is not TreeAttribute attributes) return TextCommandResult.Success("error not a TreeAttribute");
                        
            // remove food perish data
            attributes.RemoveAttribute("transitionstate");

            _config.HomeItem = new StarterkitItem(enumItemClass, code, stackSize, attributes);
            _config.MarkDirty();
            return TextCommandResult.Success(Lang.Get("th3essentials:hs-item-set"));
        }

        public static void TeleportTo(IPlayer player, Th3PlayerData playerData, BlockPos location, bool excludeFromBack = false)
        {
            if (!excludeFromBack)
            {
                playerData.LastPosition = player.Entity.Pos.AsBlockPos;
            }
            player.Entity.TeleportTo(new Vec3d(location.X + 0.5,location.Y + 0.2,location.Z + 0.5));
            playerData.HomeLastuseage = DateTime.Now;
            playerData.MarkDirty();
        }

        public static bool CanTravel(Th3PlayerData playerData, int overrideCooldown = -1)
        {
            var cooldown = overrideCooldown >= 0 ? overrideCooldown : Th3Essentials.Config.HomeCooldown;
            var canTravel = playerData.HomeLastuseage.AddSeconds(cooldown);
            return canTravel <= DateTime.Now;
        }

        public int GetPlayerHomeLimit(IPlayer callerPlayer, Th3PlayerData th3PlayerData)
        {
            if (_config.RoleConfig != null && _config.RoleConfig.TryGetValue(callerPlayer.Role.Code, out var config))
            {
                return th3PlayerData.HomeLimit >= 0 ? th3PlayerData.HomeLimit : config.HomeLimit >= 0 ? config.HomeLimit : _config.HomeLimit;
            }
            return th3PlayerData.HomeLimit >= 0 ? th3PlayerData.HomeLimit : _config.HomeLimit;
        }

        public static RoleConfig GetConfig(IPlayer player, Th3PlayerData th3PlayerData, Th3Config th3config)
        {
            if (th3config.RoleConfig != null && th3config.RoleConfig.TryGetValue(player.Role.Code, out var config))
            {
                config.HomeLimit = th3PlayerData.HomeLimit >= 0 ? th3PlayerData.HomeLimit : config.HomeLimit >= 0 ? config.HomeLimit : th3config.HomeLimit;
                config.TeleportToPlayerCost = config.TeleportToPlayerCost >= 0
                    ? config.TeleportToPlayerCost
                    : th3config.TeleportToPlayerItem?.Stacksize ?? 0; 
                config.RandomTeleportCost = config.RandomTeleportCost >= 0
                    ? config.RandomTeleportCost
                    : th3config.RandomTeleportItem?.Stacksize ?? 0; 
                config.HomeTeleportCost = config.HomeTeleportCost >= 0
                    ? config.HomeTeleportCost
                    : th3config.HomeItem?.Stacksize ?? 0; 
                config.BackTeleportCost =  config.BackTeleportCost >= 0
                    ? config.BackTeleportCost
                    : th3config.HomeItem?.Stacksize ?? 0; 
                config.SetHomeCost =  config.SetHomeCost >= 0
                    ? config.SetHomeCost
                    : th3config.HomeItem?.Stacksize ?? 0;
                return config;
            }

            return new RoleConfig
            {
                HomeLimit = th3PlayerData.HomeLimit >= 0 ? th3PlayerData.HomeLimit : th3config.HomeLimit,
                TeleportToPlayerCost = th3config.TeleportToPlayerItem?.Stacksize ?? 0,
                RandomTeleportCost = th3config.RandomTeleportItem?.Stacksize ?? 0,
                HomeTeleportCost = th3config.HomeItem?.Stacksize ?? 0,
                SetHomeCost = th3config.SetHomeItem?.Stacksize ?? 0,
                BackTeleportCost = th3config.HomeItem?.Stacksize ?? 0,
                RtpEnabled = th3config.RandomTeleportRadius >= 0,
                TeleportToPlayerEnabled = th3config.TeleportToPlayerEnabled
            };
        }
    }
}