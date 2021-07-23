using System;
using System.Collections.Generic;
using Th3Essentials.Homepoints;
using Vintagestory.API.MathTools;

namespace Th3Essentials.PlayerData
{
    public class Th3PlayerData
    {
        public static int DefaultHomeLimit;

        public static int DefaultHomeCooldown;

        public string PlayerUID;

        public int HomeLimit;

        public int HomeCooldown;

        public DateTime HomeLastuseage;

        public bool StarterkitRecived;

        public BlockPos LastPosition;

        public List<HomePoint> HomePoints;

        public Th3PlayerData()
        {
            StarterkitRecived = false;
            HomeLimit = DefaultHomeLimit;
            HomeCooldown = DefaultHomeCooldown;
            HomePoints = new List<HomePoint>();
        }

        public Th3PlayerData(string playerUID) : this()
        {
            PlayerUID = playerUID;
        }

        public bool HasMaxHomes()
        {
            return HomePoints.Count >= HomeLimit;
        }

        public HomePoint FindPointByName(string name)
        {
            return HomePoints.Find(point => point.Name == name);
        }
    }
}