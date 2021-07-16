using Vintagestory.API.Common;

namespace Th3Essentials.Starterkit
{
    public class StarterkitItem
    {
        public EnumItemClass itemclass;

        public AssetLocation code;

        public int stacksize;

        public StarterkitItem(EnumItemClass itemclass, AssetLocation code, int stacksize)
        {
            this.itemclass = itemclass;
            this.code = code;
            this.stacksize = stacksize;
        }

        public override string ToString()
        {
            return $"EnumItemClass: {itemclass}, AssetLocation: {code}, stacksize: {stacksize}";
        }
    }
}