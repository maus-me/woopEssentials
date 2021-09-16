using System;
using System.Collections.Generic;
using ProtoBuf;
using Th3Essentials.Homepoints;
using Vintagestory.API.MathTools;

namespace Th3Essentials.PlayerData
{
    [ProtoContract]
    public class Th3PlayerData
    {
        public bool IsDirty;

        [ProtoMember(1)]
        public DateTime HomeLastuseage;

        [ProtoMember(2)]
        public bool StarterkitRecived;

        [ProtoMember(3)]
        public BlockPos LastPosition;

        [ProtoMember(4)]
        public List<HomePoint> HomePoints;

        public Th3PlayerData()
        {
            HomePoints = new List<HomePoint>();
        }

        public HomePoint FindPointByName(string name)
        {
            return HomePoints.Find(point => point.Name == name);
        }

        internal void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }
    }
}