using System;
using System.Collections.Generic;
using CBSEssentials.Homepoints;

namespace CBSEssentials.PlayerData
{
    public class CBSPlayerData
    {
        public string playerUID;
        public int homeLimit;
        public int homeCooldown;
        public DateTime homeLastuseage;
        public DateTime starterkitRecived;
        public List<HomePoint> homePoints;

        public CBSPlayerData()
        {
            this.homeLimit = 6;
            this.homeCooldown = 5;
            this.homePoints = new List<HomePoint>();
        }

        public CBSPlayerData(string playerUID) : this()
        {
            this.playerUID = playerUID;
        }

        public bool hasMaxHomes()
        {
            return homePoints.Count >= homeLimit;
        }

        public HomePoint findPointByName(string name)
        {
            for (int i = 0; i < homePoints.Count; i++)
            {
                if (homePoints[i].name == name)
                {
                    return homePoints[i];
                }

            }
            return null;
        }
    }
}