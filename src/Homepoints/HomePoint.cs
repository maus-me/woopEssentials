using Vintagestory.API.MathTools;

namespace Th3Essentials.Homepoints
{
    public class HomePoint
    {
        public string name;

        public BlockPos position;

        public HomePoint(string name, BlockPos position)
        {
            this.name = name;
            this.position = position;
        }
    }
}