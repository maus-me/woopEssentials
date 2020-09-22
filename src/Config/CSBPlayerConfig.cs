using System.Collections.Generic;
using CBSEssentials.PlayerData;

namespace CBSEssentials.Config
{
    public class CBSPlayerConfig
    {
        public List<CBSPlayerData> players;

        public CBSPlayerData getPlayerDataByUID(string playerUID)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].playerUID == playerUID)
                {
                    return players[i];
                }
            }
            return null;
        }

        public bool recivedStarterkitByUID(string playerUID)
        {
            return getPlayerDataByUID(playerUID)?.starterkitRecived != null;
        }
    }
}