using System.Collections.Generic;
using Th3Essentials.PlayerData;

namespace Th3Essentials.Config
{
    public class Th3PlayerConfig
    {
        public List<Th3PlayerData> Players;

        public Th3PlayerConfig()
        {
            Players = new List<Th3PlayerData>();
        }

        public Th3PlayerData GetPlayerDataByUID(string playerUID)
        {
            return Players.Find(player => player.PlayerUID == playerUID);
        }
    }
}