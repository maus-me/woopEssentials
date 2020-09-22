using System;
using System.Collections.Generic;

namespace CBSEssentials.Homepoints
{
    public class PlayerHome
    {
        public string playerUID;
        public string playername;
        public DateTime lastUse;
        public List<Point> points;

        public PlayerHome()
        {
            points = new List<Point>();
            lastUse = new DateTime();
        }

        public PlayerHome(string playerUID, string playername) : this()
        {
            this.playername = playername;
            this.playerUID = playerUID;
        }

        public Point findPointByName(string name)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].name == name)
                {
                    return points[i];
                }

            }
            return null;
        }

        public bool hasMaxHomes(int maxhomes)
        {
            return points.Count >= maxhomes;
        }
    }
}