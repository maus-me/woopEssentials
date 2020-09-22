using Vintagestory.API.MathTools;

namespace CBSEssentials.Homepoints
{
    public class HomePoint
    {
        public string name { get; set; }

        public Vec3d position { get; set; }

        public HomePoint(string name, Vec3d position)
        {
            this.name = name;
            this.position = position;
        }
    }
}