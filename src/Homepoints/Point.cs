using Vintagestory.API.MathTools;

namespace CBSEssentials.Homepoints
{
    public class Point
    {
        public string name { get; set; }

        public Vec3d position { get; set; }

        public Point(string name, Vec3d position)
        {
            this.name = name;
            this.position = position;
        }
    }
}