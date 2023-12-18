using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Th3Essentials.Config;

public class StarterkitItem
{
    public EnumItemClass Itemclass;

    public AssetLocation Code = null!;

    public int Stacksize;

    public byte[]? Attributes;

    // ReSharper disable once UnusedMember.Global
    public StarterkitItem() { }

    public StarterkitItem(EnumItemClass itemclass, AssetLocation code, int stacksize, TreeAttribute? attributes = null)
    {
        Itemclass = itemclass;
        Code = code;
        Stacksize = stacksize;
        Attributes = attributes?.ToBytes();
    }

    public override string ToString()
    {
        return $"EnumItemClass: {Itemclass}, AssetLocation: {Code}, stacksize: {Stacksize}";
    }
}