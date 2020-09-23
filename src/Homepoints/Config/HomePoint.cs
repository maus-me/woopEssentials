using Vintagestory.API.MathTools;

namespace CBSEssentials.Homepoints
{
    public class HomePoint
    {
        public string name { get; set; }

        public BlockPos position { get; set; }

        public HomePoint(string name, BlockPos position)
        {
            this.name = name;
            this.position = position;
        }
    }
}