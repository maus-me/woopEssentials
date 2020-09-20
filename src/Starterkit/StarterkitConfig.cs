using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace CBSEssentials.Starterkit
{
    public class StarterkitConfig
    {

        public int modifyPrivilegeLevel;
        public List<StarterkitItem> items;

        public List<StarterkitPlayer> playersRecived;

        public StarterkitConfig()
        {
            playersRecived = new List<StarterkitPlayer>();
            items = new List<StarterkitItem>();
            modifyPrivilegeLevel = 99999;
        }

        public bool hasPlayerRecived(string playerUID)
        {
            foreach (StarterkitPlayer starterkitplayer in playersRecived)
            {
                if (starterkitplayer.playerUID == playerUID)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class StarterkitPlayer
    {
        public string playerUID;

        public string playername;

        public DateTime recived;

        public StarterkitPlayer(string playerUID, string playerName)
        {
            this.playerUID = playerUID;
            playername = playerName;
            recived = DateTime.Now;
        }
    }

    public class StarterkitItem
    {
        public EnumItemClass itemclass;

        public AssetLocation code;

        public int stacksize;

        public StarterkitItem(EnumItemClass itemclass, AssetLocation code, int stacksize)
        {
            this.itemclass = itemclass;
            this.code = code;
            this.stacksize = stacksize;
        }

        public override string ToString()
        {
            return $"EnumItemClass: {itemclass}, AssetLocation: {code}, stacksize: {stacksize}";
        }
    }
}