using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Th3Essentials.Config
{
    public class Th3PlayerConfig
    {
        public Dictionary<string, Th3PlayerData> Players;

        public Th3PlayerConfig()
        {
            Players = new Dictionary<string, Th3PlayerData>();
        }

        /// <summary>
        /// gets or creates a Th3PlayerData object with given UID
        /// </summary>
        /// <param name="playerUID"></param>
        /// <returns>Th3PlayerData</returns>
        public Th3PlayerData GetPlayerDataByUID(string playerUID, bool shouldCreate = true)
        {
            if (!Players.TryGetValue(playerUID, out Th3PlayerData playerData) && shouldCreate)
            {
                playerData = new Th3PlayerData();
                playerData.MarkDirty();
                Add(playerUID, playerData);
            }
            return playerData;
        }

        internal void GameWorldSave(ICoreServerAPI api)
        {
            foreach (KeyValuePair<string, Th3PlayerData> playerData in Players)
            {
                if (playerData.Value.IsDirty)
                {
                    playerData.Value.IsDirty = false;
                    byte[] data = SerializerUtil.Serialize(playerData.Value);
                    IPlayer player = api.World.PlayerByUid(playerData.Key);
                    player.WorldData.SetModdata(Th3Essentials.Th3EssentialsModDataKey, data);
                }
            }
        }

        public void Add(string PlayerUID, Th3PlayerData playerData)
        {
            Players.Add(PlayerUID, playerData);
        }
    }
}