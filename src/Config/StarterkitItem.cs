using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Th3Essentials.Starterkit
{
  public class StarterkitItem
  {
    public EnumItemClass Itemclass;

    public AssetLocation Code;

    public int Stacksize;

    public byte[] Attributes;

    public StarterkitItem() { }

    public StarterkitItem(EnumItemClass itemclass, AssetLocation code, int stacksize, TreeAttribute attributes = null)
    {
      Itemclass = itemclass;
      Code = code;
      Stacksize = stacksize;
      Attributes = attributes.ToBytes();
    }

    public override string ToString()
    {
      return $"EnumItemClass: {Itemclass}, AssetLocation: {Code}, stacksize: {Stacksize}";
    }
  }
}