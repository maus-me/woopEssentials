using System;
using CBSEssentials.Config;
using CBSEssentials.PlayerData;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CBSEssentials.Starterkit
{
    internal class Starterkitsystem
    {
        public CBSConfig config;
        
        public CBSPlayerConfig playerConfig;

        internal void Init(ICoreServerAPI api)
        {
            config = CBSEssentials.Config;
            playerConfig = CBSEssentials.PlayerConfig;
            RegisterCommands(api);
        }

        private void RegisterCommands(ICoreServerAPI api)
        {
            api.RegisterCommand("starterkit", Lang.Get("cbsessentials:cd-starterkit"), string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    TryGiveItemStack(api, player);
                }, Privilege.chat);

            api.RegisterCommand("setstarterkit", Lang.Get("cbsessentials:cd-setstarterkit"), string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                config.items.Clear();
                IInventory inventory = player.InventoryManager.GetHotbarInventory();
                for (int i = 0; i < inventory.Count; i++)
                {
                    if (inventory[i].GetType() == typeof(ItemSlotSurvival) && inventory[i].Itemstack != null)
                    {
                        EnumItemClass enumItemClass = inventory[i].Itemstack.Class;
                        int stackSize = inventory[i].Itemstack.StackSize;
                        AssetLocation code = inventory[i].Itemstack.Collectible.Code;
                        config.items.Add(new StarterkitItem(enumItemClass, code, stackSize));
                    }
                }
            }, Privilege.controlserver);
        }

        private void TryGiveItemStack(ICoreServerAPI api, IServerPlayer player)
        {
            CBSPlayerData playerData = playerConfig.GetPlayerDataByUID(player.PlayerUID);
            if (playerData != null && playerData.GotStarterkit())
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:st-hasalready"), EnumChatType.Notification);
            }
            else
            {
                if (config.items.Count == 0)
                {
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:st-notsetup"), EnumChatType.Notification);
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
                    if (emptySlots < config.items.Count)
                    {
                        player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:st-needspace", config.items.Count), EnumChatType.Notification);
                        return;
                    }
                    for (int i = 0; i < config.items.Count; i++)
                    {
                        AssetLocation asset = new AssetLocation(config.items[i].code.Path);
                        if (asset != null)
                        {
                            bool recived = false;
                            switch (config.items[i].itemclass)
                            {
                                case EnumItemClass.Item:
                                    {
                                        Item item = api.World.GetItem(asset);
                                        if (item != null)
                                        {
                                            recived = player.Entity.TryGiveItemStack(new ItemStack(item, config.items[i].stacksize));
                                        }
                                        break;
                                    }
                                case EnumItemClass.Block:
                                    {
                                        Block block = api.World.GetBlock(asset);
                                        if (block != null)
                                        {
                                            recived = player.Entity.TryGiveItemStack(new ItemStack(block, config.items[i].stacksize));
                                        }
                                        break;
                                    }
                            }
                            if (!recived)
                            {
                                player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:st-wrong"), EnumChatType.Notification);
                                throw new Exception($"Could not give item/block: {config.items[i]}");
                            }
                        }
                    }
                    player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("cbsessentials:st-recived"), EnumChatType.Notification);

                    if (playerData != null)
                    {
                        playerData.homeLastuseage = DateTime.Now;
                    }
                    else
                    {
                        playerData = new CBSPlayerData(player.PlayerUID)
                        {
                            homeLastuseage = DateTime.Now
                        };
                        playerConfig.players.Add(playerData);
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
