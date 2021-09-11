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
        public bool IsDirty { get; internal set; }

        public string PlayerUID;

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

        public Th3PlayerData(string playerUID) : this()
        {
            PlayerUID = playerUID;
            IsDirty = true;
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

    [ProtoContract]
    public class Th3PlayerDataOld
    {
        public bool IsDirty { get; internal set; }

        public string PlayerUID;

        [ProtoMember(1)]
        public int HomeLimit;

        [ProtoMember(2)]
        public int HomeCooldown;

        [ProtoMember(3)]
        public DateTime HomeLastuseage;

        [ProtoMember(4)]
        public bool StarterkitRecived;

        [ProtoMember(5)]
        public BlockPos LastPosition;

        [ProtoMember(6)]
        public List<HomePoint> HomePoints;

        public Th3PlayerDataOld()
        {
            HomePoints = new List<HomePoint>();
        }

        internal static Th3PlayerData Convert(Th3PlayerDataOld pold)
        {
            return new Th3PlayerData()
            {
                HomeLastuseage = pold.HomeLastuseage,
                StarterkitRecived = pold.StarterkitRecived,
                LastPosition = pold.LastPosition,
                HomePoints = pold.HomePoints
            };
        }
    }
}