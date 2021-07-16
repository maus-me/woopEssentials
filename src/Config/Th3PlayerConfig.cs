using System.Collections.Generic;
using Th3Essentials.PlayerData;

namespace Th3Essentials.Config
{
    public class Th3PlayerConfig
    {
        public List<Th3PlayerData> players;

        public Th3PlayerConfig()
        {
            players = new List<Th3PlayerData>();
        }

        public Th3PlayerData GetPlayerDataByUID(string playerUID)
        {
            return players.Find(player => player.playerUID == playerUID);
        }
    }
}