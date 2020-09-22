using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CBSEssentials.Starterkit
{
    internal class Starterkitsystem
    {
        private StarterkitConfig config;

        private const string configFile = "starterkitsystem.json";

        internal void init(ICoreServerAPI api)
        {
            config = api.LoadModConfig<StarterkitConfig>(configFile);

            if (config == null)
            {
                config = new StarterkitConfig();
                api.StoreModConfig(config, configFile);
                api.Server.LogWarning("Starterkitsystem initialized with default config!!!");
                api.Server.LogWarning("Starterkitsystem config file at " + Path.Combine(GamePaths.ModConfig, configFile));
            }
            registerCommands(api);
        }

        private void registerCommands(ICoreServerAPI api)
        {
            api.RegisterCommand("starterkit", "Gibt dir ein einmaliges Starterkit", string.Empty,
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    tryGiveItemStack(api, player);
                }, Privilege.chat);

            api.RegisterCommand("resetstarterkit", "reset starterkit config to default", string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                config = new StarterkitConfig();
                api.StoreModConfig(config, configFile);
            }, Privilege.controlserver);

            api.RegisterCommand("setstarterkit", "set starterkit to items on your hotbar", string.Empty,
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

                api.StoreModConfig(config, configFile);
            }, Privilege.controlserver);
        }

        private void tryGiveItemStack(ICoreServerAPI api, IServerPlayer player)
        {
            if (config.hasPlayerRecived(player.PlayerUID))
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, "Du hast bereits ein Starterkit bekommen.", EnumChatType.Notification);
            }
            else
            {
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
                        player.SendMessage(GlobalConstants.GeneralChatGroup, "Du hast nicht genügend Platz im Inventar.", EnumChatType.Notification);
                        api.Server.LogVerboseDebug($"Starterkit player has not enough empty slots: {config.items.Count} Slots needed but has {emptySlots}");
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
                                player.SendMessage(GlobalConstants.GeneralChatGroup, "Irgendetwas lief schief mit dem Starterkit, bitte informieren einen Mod/Admin", EnumChatType.Notification);
                                throw new Exception($"Could not give item/block: {config.items[i]}");
                            }
                        }
                    }
                    player.SendMessage(GlobalConstants.GeneralChatGroup, "Hier dein Starterkit :)", EnumChatType.Notification);
                    config.playersRecived.Add(new StarterkitPlayer(player.PlayerUID, player.PlayerName));
                    api.StoreModConfig(config, configFile);
                }
                catch (Exception e)
                {
                    api.Server.LogError(e.Message);
                }
            }
        }
    }
}
