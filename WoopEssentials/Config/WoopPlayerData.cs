using System;
using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.MathTools;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace WoopEssentials.Config;

[ProtoContract]
public class WoopPlayerData
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
    
    [ProtoMember(9)]
    public List<Mail> Mails = new();

    // Bed spawn position (block coordinates). When null, no bed spawn is set
    [ProtoMember(10)]
    public BlockPos? BedPos;

    // Cumulative play time tracking for auto-whitelist (in seconds)
    [ProtoMember(11)]
    public double TotalPlaySeconds;

    // Whether the player has already been auto-whitelisted
    [ProtoMember(12)]
    public bool AutoWhitelisted;

    // Last login time to accumulate session duration (UTC)
    [ProtoMember(13)]
    public DateTime LastJoinUtc;

    [ProtoMember(14)]
    public DateTime FirstJoinUtc;

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
