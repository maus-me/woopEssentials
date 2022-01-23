using System;
using System.Collections.Generic;
using Th3Essentials.Config;
using Th3Essentials.PlayerData;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

namespace Th3Essentials.Starterkit
{
    internal class Starterkitsystem
    {
        private Th3Config _config;

        private Th3PlayerConfig _playerConfig;

        internal void Init(ICoreServerAPI sapi)
        {
            _config = Th3Essentials.Config;
            _playerConfig = Th3Essentials.PlayerConfig;
            RegisterCommands(sapi);
        }

        private void RegisterCommands(ICoreServerAPI sapi)
        {
            _ = sapi.RegisterCommand("starterkit", Lang.Get("th3essentials:cd-starterkit"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    TryGiveItemStack(sapi, player);
                }, Privilege.chat);

            _ = sapi.RegisterCommand("setstarterkit", Lang.Get("th3essentials:cd-setstarterkit"), string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                if (_config.Items == null)
                {
                    _config.Items = new List<StarterkitItem>();
                }
                else
                {
                    _config.Items.Clear();
                }
                IInventory inventory = player.InventoryManager.GetHotbarInventory();
                for (int i = 0; i < inventory.Count; i++)
                {
                    if (inventory[i].GetType() == typeof(ItemSlotSurvival) && inventory[i].Itemstack != null)
                    {
                        EnumItemClass enumItemClass = inventory[i].Itemstack.Class;
                        int stackSize = inventory[i].Itemstack.StackSize;
                        AssetLocation code = inventory[i].Itemstack.Collectible.Code;
                        TreeAttribute attributes = inventory[i].Itemstack.Attributes as TreeAttribute;
                        // remove food persih data
                        attributes.RemoveAttribute("transitionstate");

                        _config.Items.Add(new StarterkitItem(enumItemClass, code, stackSize, attributes));
                    }
                }
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-setup"), EnumChatType.CommandSuccess);
            }, Privilege.controlserver);

            _ = sapi.RegisterCommand("resetstarterkitusageall", Lang.Get("th3essentials:cd-rstall"), string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                string ok = args.PopWord();
                if (ok != null && ok == "confirm")
                {
                    ServerMain server = (ServerMain)sapi.World;
                    GameDatabase gameDatabase = new GameDatabase(ServerMain.Logger);
                    _ = gameDatabase.ProbeOpenConnection(server.GetSaveFilename(), true, out int foundVersion, out string errorMessage, out bool isReadonly);
                    gameDatabase.UpgradeToWriteAccess();

                    foreach (ServerPlayerData th3d in server.PlayerDataManager.PlayerDataByUid.Values)
                    {
                        Th3PlayerData onwdata = _playerConfig.GetPlayerDataByUID(th3d.PlayerUID, false);
                        if (onwdata != null)
                        {
                            onwdata.StarterkitRecived = false;
                            onwdata.MarkDirty();
                            sapi.Logger.Debug("Starterkit for {0} was reset", th3d.LastKnownPlayername);
                        }
                        else
                        {
                            ServerWorldPlayerData swpdata = SerializerUtil.Deserialize<ServerWorldPlayerData>(gameDatabase.GetPlayerData(th3d.PlayerUID));
                            Th3PlayerData th3pdata = SerializerUtil.Deserialize<Th3PlayerData>(swpdata.GetModdata(Th3Essentials.Th3EssentialsModDataKey), null);
                            if (th3pdata != null)
                            {
                                th3pdata.StarterkitRecived = false;
                                swpdata.SetModdata(Th3Essentials.Th3EssentialsModDataKey, SerializerUtil.Serialize(th3pdata));
                                gameDatabase.SetPlayerData(th3d.PlayerUID, SerializerUtil.Serialize(swpdata));
                            }
                            else
                            {
                                sapi.Logger.Debug("No Th3PlayerData for player {0} found", th3d.LastKnownPlayername);
                            }
                        }
                    }
                    gameDatabase.Dispose();
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-rst-alldone"), EnumChatType.CommandSuccess);
                }
                else
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-rst"), EnumChatType.CommandSuccess);
                }
            }, Privilege.controlserver);

            _ = sapi.RegisterCommand("resetstarterkitusage", Lang.Get("th3essentials:cd-rstp"), "[Name]",
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                string name = args.PopWord();
                if (name != null)
                {
                    IServerPlayerData foundPlayer = sapi.PlayerData.GetPlayerDataByLastKnownName(name);
                    if (foundPlayer != null)
                    {
                        Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(foundPlayer.PlayerUID, false);
                        if (playerData != null)
                        {
                            playerData.StarterkitRecived = false;
                            playerData.MarkDirty();
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-rstp-done", foundPlayer.LastKnownPlayername), EnumChatType.CommandSuccess);
                        }
                        else
                        {
                            player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-rstp-npd"), EnumChatType.CommandError);
                        }
                    }
                    else
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:cd-rstp-unknown"), EnumChatType.CommandError);
                    }
                }
                else
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "/resetstarterkitusage [Name]", EnumChatType.CommandError);
                }
            }, Privilege.controlserver);
        }

        private void TryGiveItemStack(ICoreServerAPI api, IServerPlayer player)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData.StarterkitRecived)
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-hasalready"), EnumChatType.CommandSuccess);
            }
            else
            {
                if (_config.Items == null || _config.Items.Count == 0)
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-notsetup"), EnumChatType.CommandSuccess);
                    return;
                }
                try
                {
                    int emptySlots = 0;
                    IInventory inventory = player.InventoryManager.GetHotbarInventory();
                    for (int i = 0; i < inventory.Count; i++)
                    {
                        if (inventory[i].GetType() == typeof(ItemSlotSurvival) && inventory[i].Empty)
                        {
                            emptySlots++;
                        }
                    }
                    if (emptySlots < _config.Items.Count)
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-needspace", _config.Items.Count), EnumChatType.CommandSuccess);
                        return;
                    }
                    for (int i = 0; i < _config.Items.Count; i++)
                    {
                        AssetLocation asset = new AssetLocation(_config.Items[i].Code.Path);
                        if (asset != null)
                        {
                            bool recived = false;
                            switch (_config.Items[i].Itemclass)
                            {
                                case EnumItemClass.Item:
                                    {
                                        Item item = api.World.GetItem(asset);

                                        if (item != null)
                                        {
                                            ItemStack itemStack = new ItemStack(item, _config.Items[i].Stacksize)
                                            {
                                                Attributes = TreeAttribute.CreateFromBytes(_config.Items[i].Attributes)
                                            };

                                            recived = player.Entity.TryGiveItemStack(itemStack);
                                        }
                                        break;
                                    }
                                case EnumItemClass.Block:
                                    {
                                        Block block = api.World.GetBlock(asset);
                                        if (block != null)
                                        {
                                            ItemStack itemStack = new ItemStack(block, _config.Items[i].Stacksize)
                                            {
                                                Attributes = TreeAttribute.CreateFromBytes(_config.Items[i].Attributes)
                                            };

                                            recived = player.Entity.TryGiveItemStack(itemStack);
                                        }
                                        break;
                                    }

                                default:
                                    break;
                            }
                            if (!recived)
                            {
                                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-wrong"), EnumChatType.CommandError);
                                throw new Exception($"Could not give item/block: {_config.Items[i]}");
                            }
                        }
                    }
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-recived"), EnumChatType.CommandSuccess);
                    playerData.StarterkitRecived = true;
                    playerData.MarkDirty();
                }
                catch (Exception e)
                {
                    api.Server.LogError(e.Message);
                }
            }
        }
    }
}
