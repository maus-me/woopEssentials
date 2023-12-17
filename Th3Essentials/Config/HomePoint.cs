using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Th3Essentials.Config;

[ProtoContract]
public class HomePoint
{
    [ProtoMember(1)]
    public string Name;

    [ProtoMember(2)]
    public BlockPos Position;

    public HomePoint()
    { }

    public HomePoint(string name, BlockPos position)
    {
        Name = name;
        Position = position;
    }
}