using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Th3Essentials.Config;

[ProtoContract]
public class Th3PlayerData
{
    public bool IsDirty;

    [ProtoMember(1)]
    public DateTime HomeLastuseage;

    [ProtoMember(2)]
    public bool StarterkitRecived;

    [ProtoMember(3)]
    public BlockPos? LastPosition;

    [ProtoMember(4)]
    public List<HomePoint> HomePoints = new();

    [ProtoMember(5)]
    public int HomeLimit = -1;

    [ProtoMember(6)]
    // ReSharper disable once InconsistentNaming
    public DateTime RTPLastUsage;

    [ProtoMember(7)]
    public DateTime T2PLastUsage;

    [ProtoMember(8)]
    public DateTime WarpLastUsage;

    public HomePoint? FindPointByName(string name)
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