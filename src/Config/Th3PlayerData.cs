using System;
using System.Collections.Generic;
using Th3Essentials.Homepoints;

namespace Th3Essentials.PlayerData
{
    public class Th3PlayerData
    {
        public static int defaultHomeLimit = 6;

        public static int defaultHomeCooldown = 5;

        public string playerUID;

        public int homeLimit;

        public int homeCooldown;

        public DateTime homeLastuseage;

        public bool starterkitRecived;

        public List<HomePoint> homePoints;

        public Th3PlayerData()
        {
            starterkitRecived = false;
            homeLimit = defaultHomeLimit;
            homeCooldown = defaultHomeCooldown;
            homePoints = new List<HomePoint>();
        }

        public Th3PlayerData(string playerUID) : this()
        {
            this.playerUID = playerUID;
        }

        public bool HasMaxHomes()
        {
            return homePoints.Count >= homeLimit;
        }

        public HomePoint FindPointByName(string name)
        {
            return homePoints.Find(point => point.name == name);
        }
    }
}