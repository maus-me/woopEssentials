using System.Collections.Generic;
using System;
using Vintagestory.API.MathTools;

namespace CBSEssentials.Homepoints
{
    public class Homes
    {
        public int maxhomes;

        public double cooldown;

        public List<PlayerHomes> playerhomes;

        public Homes()
        {
            playerhomes = new List<PlayerHomes>();
            maxhomes = 6;
            cooldown = 5;
        }

        public PlayerHomes findPlayerhomeByUID(string playerUID)
        {
            for (int i = 0; i < playerhomes.Count; i++)
            {
                if (playerhomes[i].playerUID == playerUID)
                {
                    return playerhomes[i];
                }
            }
            return null;
        }
    }
    public class PlayerHomes
    {
        public string playerUID;
        public string playername;
        public DateTime lastUse;
        public List<Point> points;

        public PlayerHomes()
        {
            points = new List<Point>();
            lastUse = new DateTime();
        }

        public PlayerHomes(string playerUID, string playername) : this()
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

    public class Point
    {
        public string name { get; set; }

        public Vec3d position { get; set; }

        public Point(string name, Vec3d position)
        {
            this.name = name;
            this.position = position;
        }
    }
}
