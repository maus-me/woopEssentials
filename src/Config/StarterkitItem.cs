using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Th3Essentials.Starterkit
{
  public class StarterkitItem
  {
    public EnumItemClass Itemclass;

    public AssetLocation Code;

    public int Stacksize;

    public ITreeAttribute Attributes;

    public StarterkitItem(EnumItemClass itemclass, AssetLocation code, int stacksize, ITreeAttribute attributes = null)
    {
      Itemclass = itemclass;
      Code = code;
      Stacksize = stacksize;
      Attributes = attributes;
    }

    public override string ToString()
    {
      return $"EnumItemClass: {Itemclass}, AssetLocation: {Code}, stacksize: {Stacksize}";
    }
  }
}