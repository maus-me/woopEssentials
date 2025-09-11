using ProtoBuf;
using Vintagestory.API.MathTools;

namespace WoopEssentials.Config;

[ProtoContract]
public class HomePoint
{
    [ProtoMember(1)]
    public string Name = null!;

    [ProtoMember(2)]
    public BlockPos Position = null!;

    // ReSharper disable once UnusedMember.Global
    public HomePoint()
    { }

    public HomePoint(string name, BlockPos position)
    {
        Name = name;
        Position = position;
    }
}