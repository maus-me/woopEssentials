using System.Collections.Generic;

namespace CBSEssentials.Homepoints
{
    public class Homes
    {
        public int maxhomes;

        public double cooldown;

        public List<PlayerHome> playerhomes;

        public Homes()
        {
            playerhomes = new List<PlayerHome>();
            maxhomes = 6;
            cooldown = 5;
        }

        public PlayerHome findPlayerhomeByUID(string playerUID)
        {
            for (int i = 0; i < playerhomes.Count; i++)
            {
                if (playerhomes[i].playerUID == playerUID)
                {
                    return playerhomes[i];
                }
            }
            return null;
        }
    }
}