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

            api.RegisterCommand("resetstarterkit", "reset starterkit config", string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                if (player.Role.PrivilegeLevel >= 99999)
                {
                    config = new StarterkitConfig();
                    api.StoreModConfig(config, configFile);
                }
            }, Privilege.chat);

            api.RegisterCommand("setstarterkit", "setzt die Starterkit items", string.Empty,
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                if (player.Role.PrivilegeLevel >= config.modifyPrivilegeLevel)
                {
                    config.items.Clear();
                    IInventory inventory = player.InventoryManager.GetHotbarInventory();
                    for (int i = 0; i < inventory.Count; i++)
                    {
                        if (inventory[i].Itemstack != null)
                        {
                            EnumItemClass enumItemClass = inventory[i].Itemstack.Class;
                            int stackSize = inventory[i].Itemstack.StackSize;
                            AssetLocation code = inventory[i].Itemstack.Collectible.Code;
                            api.Server.LogVerboseDebug($"item: {enumItemClass} : {code} : {stackSize}");
                            api.Server.LogVerboseDebug($"item: {config.items.Count}");
                            config.items.Add(new StarterkitItem(enumItemClass, code, stackSize));
                        }
                    }
                }
                api.StoreModConfig(config, configFile);
            }, Privilege.chat);
        }

        private void tryGiveItemStack(ICoreServerAPI api, IServerPlayer player)
        {
            if (config.hasPlayerRecived(player.PlayerUID))
            {
                player.SendMessage(GlobalConstants.GeneralChatGroup, "Du hast bereits ein starterkit bekommen.", EnumChatType.Notification);
            }
            else
            {
                try
                {
                    foreach (StarterkitItem starterkitItem in config.items)
                    {
                        AssetLocation asset = new AssetLocation(starterkitItem.code.Path);
                        if (asset != null)
                        {
                            bool recived = false;
                            switch (starterkitItem.itemclass)
                            {
                                case EnumItemClass.Item:
                                    {
                                        Item item = api.World.GetItem(asset);
                                        if (item != null)
                                        {
                                            api.Server.LogVerboseDebug($"Starterkitsystem GetItem: {asset} : {item.Code.Path}");
                                            recived = player.Entity.TryGiveItemStack(new ItemStack(item, starterkitItem.stacksize));
                                        }
                                        break;
                                    }
                                case EnumItemClass.Block:
                                    {
                                        Block block = api.World.GetBlock(asset);
                                        if (block != null)
                                        {
                                            api.Server.LogVerboseDebug($"Starterkitsystem GetBlock: {asset} : {block.Code.Path}");
                                            recived = player.Entity.TryGiveItemStack(new ItemStack(block, starterkitItem.stacksize));
                                        }
                                        break;
                                    }
                            }
                            if (!recived)
                            {
                                throw new Exception($"Could not give item/block: {starterkitItem}");
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
