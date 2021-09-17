using Vintagestory.API.Common;

namespace Th3Essentials.Starterkit
{
  public class StarterkitItem
  {
    public EnumItemClass Itemclass;

    public AssetLocation Code;

    public int Stacksize;

    public StarterkitItem(EnumItemClass itemclass, AssetLocation code, int stacksize)
    {
      Itemclass = itemclass;
      Code = code;
      Stacksize = stacksize;
    }

    public override string ToString()
    {
      return $"EnumItemClass: {Itemclass}, AssetLocation: {Code}, stacksize: {Stacksize}";
    }
  }
}