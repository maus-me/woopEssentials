using System;
using Th3Essentials.Config;
using Th3Essentials.PlayerData;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Starterkit
{
    internal class Starterkitsystem
    {
        private Th3Config _config;

        private Th3PlayerConfig _playerConfig;

        internal void Init(ICoreServerAPI api)
        {
            _config = Th3Essentials.Config;
            _playerConfig = Th3Essentials.PlayerConfig;
            RegisterCommands(api);
        }

        private void RegisterCommands(ICoreServerAPI api)
        {
            api.RegisterCommand("starterkit", Lang.Get("th3essentials:cd-starterkit"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    TryGiveItemStack(api, player);
                }, Privilege.chat);

            api.RegisterCommand("setstarterkit", Lang.Get("th3essentials:cd-setstarterkit"), string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                _config.Items.Clear();
                IInventory inventory = player.InventoryManager.GetHotbarInventory();
                for (int i = 0; i < inventory.Count; i++)
                {
                    if (inventory[i].GetType() == typeof(ItemSlotSurvival) && inventory[i].Itemstack != null)
                    {
                        EnumItemClass enumItemClass = inventory[i].Itemstack.Class;
                        int stackSize = inventory[i].Itemstack.StackSize;
                        AssetLocation code = inventory[i].Itemstack.Collectible.Code;
                        _config.Items.Add(new StarterkitItem(enumItemClass, code, stackSize));
                    }
                }
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-setup"), EnumChatType.Notification);
            }, Privilege.controlserver);
        }

        private void TryGiveItemStack(ICoreServerAPI api, IServerPlayer player)
        {
            Th3PlayerData playerData = _playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null && playerData.StarterkitRecived)
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-hasalready"), EnumChatType.Notification);
            }
            else
            {
                if (_config.Items.Count == 0)
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-notsetup"), EnumChatType.Notification);
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
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-needspace", _config.Items.Count), EnumChatType.Notification);
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
                                            recived = player.Entity.TryGiveItemStack(new ItemStack(item, _config.Items[i].Stacksize));
                                        }
                                        break;
                                    }
                                case EnumItemClass.Block:
                                    {
                                        Block block = api.World.GetBlock(asset);
                                        if (block != null)
                                        {
                                            recived = player.Entity.TryGiveItemStack(new ItemStack(block, _config.Items[i].Stacksize));
                                        }
                                        break;
                                    }
                            }
                            if (!recived)
                            {
                                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-wrong"), EnumChatType.Notification);
                                throw new Exception($"Could not give item/block: {_config.Items[i]}");
                            }
                        }
                    }
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("th3essentials:st-recived"), EnumChatType.Notification);

                    if (playerData != null)
                    {
                        playerData.StarterkitRecived = true;
                    }
                    else
                    {
                        playerData = new Th3PlayerData(player.PlayerUID)
                        {
                            StarterkitRecived = true
                        };
                        _playerConfig.Players.Add(playerData);
                    }
                }
                catch (Exception e)
                {
                    api.Server.LogError(e.Message);
                }
            }
        }
    }
}
