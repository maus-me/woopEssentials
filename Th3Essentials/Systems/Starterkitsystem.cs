using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Th3Essentials.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

namespace Th3Essentials.Systems
{
    internal class Starterkitsystem
    {
        private Th3Config _config;

        private Th3PlayerConfig _playerConfig;
        private ICoreServerAPI _sapi;

        internal void Init(ICoreServerAPI sapi)
        {
            _config = Th3Essentials.Config;
            _playerConfig = Th3Essentials.PlayerConfig;
            RegisterCommands(sapi);
        }

        private void RegisterCommands(ICoreServerAPI sapi)
        {
            _sapi = sapi;
            sapi.ChatCommands.Create("starterkit")
                .WithDescription(Lang.Get("th3essentials:cd-starterkit"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.chat)
                .HandleWith(args => TryGiveItemStack(sapi, args.Caller.Player as IServerPlayer));
            
            sapi.ChatCommands.Create("setstarterkit")
                .WithDescription(Lang.Get("th3essentials:cd-setstarterkit"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(OnSetStarterKit);
            
            sapi.ChatCommands.Create("resetstarterkitusageall")
                .WithDescription(Lang.Get("th3essentials:cd-rstall"))
                .RequiresPrivilege(Privilege.controlserver)
                .WithArgs(sapi.ChatCommands.Parsers.OptionalWord("confirm"))
                .HandleWith(OnResetAllKits);

            sapi.ChatCommands.Create("resetstarterkitusage")
                .WithDescription(Lang.Get("th3essentials:cd-rstp"))
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.controlserver)
                .WithArgs(sapi.ChatCommands.Parsers.OnlinePlayer("player"))
                .HandleWith(OnResetKit);
            
      
        }

        private TextCommandResult OnResetKit(TextCommandCallingArgs args)
        {
            if (args.Parsers[0].GetValue() is IPlayer foundPlayer)
            {
                var playerData = _playerConfig.GetPlayerDataByUID(foundPlayer.PlayerUID, false);
                if (playerData != null)
                {
                    playerData.StarterkitRecived = false;
                    playerData.MarkDirty();
                    return TextCommandResult.Success(Lang.Get("th3essentials:cd-rstp-done", foundPlayer.PlayerName));
                }

                return TextCommandResult.Error(Lang.Get("th3essentials:cd-rstp-npd"));
            }
            return TextCommandResult.Error(Lang.Get("th3essentials:cd-rstp-unknown"));
        }

        private TextCommandResult OnResetAllKits(TextCommandCallingArgs args)
        {
                if (args.Parsers[0].GetValue() is not string ok || ok != "confirm")
                    return TextCommandResult.Success(Lang.Get("th3essentials:cd-rst"));

                var server = (ServerMain)_sapi.World;
                var chunkThread = typeof(ServerMain).GetField("chunkThread", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(server) as ChunkServerThread;
                var gameDatabase = typeof(ChunkServerThread).GetField("gameDatabase", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(chunkThread) as GameDatabase;
                
                foreach (var th3d in server.PlayerDataManager.PlayerDataByUid.Values)
                {
                    var onwdata = _playerConfig.GetPlayerDataByUID(th3d.PlayerUID, false);
                    if (onwdata != null)
                    {
                        onwdata.StarterkitRecived = false;
                        onwdata.MarkDirty();
                        _sapi.Logger.Debug("Starterkit for {0} was reset", th3d.LastKnownPlayername);
                    }
                    else
                    {
                        var swpdata = SerializerUtil.Deserialize<ServerWorldPlayerData>(gameDatabase.GetPlayerData(th3d.PlayerUID));
                        var th3pdata = SerializerUtil.Deserialize<Th3PlayerData>(swpdata.GetModdata(Th3Essentials.Th3EssentialsModDataKey), null);
                        if (th3pdata != null)
                        {
                            th3pdata.StarterkitRecived = false;
                            swpdata.SetModdata(Th3Essentials.Th3EssentialsModDataKey, SerializerUtil.Serialize(th3pdata));
                            gameDatabase.SetPlayerData(th3d.PlayerUID, SerializerUtil.Serialize(swpdata));
                        }
                        else
                        {
                            _sapi.Logger.Debug("No Th3PlayerData for player {0} found", th3d.LastKnownPlayername);
                        }
                    }
                }
                gameDatabase.Dispose();
                return  TextCommandResult.Success(Lang.Get("th3essentials:cd-rst-alldone"));
        }

        private TextCommandResult OnSetStarterKit(TextCommandCallingArgs args)
        {
                if (_config.Items == null)
                {
                    _config.Items = new List<StarterkitItem>();
                }
                else
                {
                    _config.Items.Clear();
                }
                var inventory = args.Caller.Player.InventoryManager.GetHotbarInventory();
                foreach (var slot in inventory)
                {
                    if (slot.GetType() != typeof(ItemSlotSurvival) || slot.Itemstack == null) continue;
                        
                    var enumItemClass = slot.Itemstack.Class;
                    var stackSize = slot.Itemstack.StackSize;
                    var code = slot.Itemstack.Collectible.Code;

                    if (slot.Itemstack.Attributes is not TreeAttribute attributes) continue;
                        
                    // remove food perish data
                    attributes.RemoveAttribute("transitionstate");

                    _config.Items.Add(new StarterkitItem(enumItemClass, code, stackSize, attributes));
                }
                _config.MarkDirty();
                return TextCommandResult.Success(Lang.Get("th3essentials:st-setup"));
        }

        private TextCommandResult TryGiveItemStack(ICoreServerAPI api, IServerPlayer player)
        {
            var playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData.StarterkitRecived)
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:st-hasalready"));
            }

            if (_config.Items == null || _config.Items.Count == 0)
            {
                return TextCommandResult.Success(Lang.Get("th3essentials:st-notsetup"));
            }
            try
            {
                var inventory = player.InventoryManager.GetHotbarInventory();
                var emptySlots = inventory.Count(slot => slot.GetType() == typeof(ItemSlotSurvival) && slot.Empty);
                if (emptySlots < _config.Items.Count)
                {
                    return TextCommandResult.Success(Lang.Get("th3essentials:st-needspace", _config.Items.Count));
                }
                for (var i = 0; i < _config.Items.Count; i++)
                {
                    var asset = new AssetLocation(_config.Items[i].Code.ToString());
                    
                    var received = false;
                    switch (_config.Items[i].Itemclass)
                    {
                        case EnumItemClass.Item:
                        {
                            var item = api.World.GetItem(asset);

                            if (item != null)
                            {
                                var itemStack = new ItemStack(item, _config.Items[i].Stacksize)
                                {
                                    Attributes = TreeAttribute.CreateFromBytes(_config.Items[i].Attributes)
                                };

                                received = player.Entity.TryGiveItemStack(itemStack);
                            }
                            break;
                        }
                        case EnumItemClass.Block:
                        {
                            var block = api.World.GetBlock(asset);
                            if (block != null)
                            {
                                var itemStack = new ItemStack(block, _config.Items[i].Stacksize)
                                {
                                    Attributes = TreeAttribute.CreateFromBytes(_config.Items[i].Attributes)
                                };

                                received = player.Entity.TryGiveItemStack(itemStack);
                            }
                            break;
                        }
                    }
                    if (!received)
                    {
                        return TextCommandResult.Error(Lang.Get("th3essentials:st-wrong"));
                    }
                }
                playerData.StarterkitRecived = true;
                playerData.MarkDirty();
                return TextCommandResult.Success(Lang.Get("th3essentials:st-recived"));
            }
            catch (Exception e)
            {
                api.Server.LogError(e.Message);
                return TextCommandResult.Error(e.Message);
            }
        }
    }
}
