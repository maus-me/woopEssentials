using System;
using System.Collections.Generic;
using ProtoBuf;
using Th3Essentials.Homepoints;
using Vintagestory.API.MathTools;

namespace Th3Essentials.PlayerData
{
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

        public Th3PlayerData Convert()
        {
            return new Th3PlayerData()
            {
                HomeLastuseage = HomeLastuseage,
                StarterkitRecived = StarterkitRecived,
                LastPosition = LastPosition,
                HomePoints = HomePoints
            };
        }
    }
}