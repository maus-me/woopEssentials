using Vintagestory.API.MathTools;

namespace Th3Essentials.Homepoints
{
    public class HomePoint
    {
        public string Name;

        public BlockPos Position;

        public HomePoint(string name, BlockPos position)
        {
            Name = name;
            Position = position;
        }
    }
}