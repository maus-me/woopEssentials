using System.Collections.Generic;
using Th3Essentials.PlayerData;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Th3Essentials.Config
{
    public class Th3PlayerConfig
    {
        public readonly List<Th3PlayerData> Players;

        public Th3PlayerConfig()
        {
            Players = new List<Th3PlayerData>();
        }

        /// <summary>
        /// gets or creates a Th3PlayerData object with given UID
        /// </summary>
        /// <param name="playerUID"></param>
        /// <returns>Th3PlayerData</returns>
        public Th3PlayerData GetPlayerDataByUID(string playerUID, bool shouldCreate = true)
        {
            Th3PlayerData playerData = Players.Find(player => player.PlayerUID == playerUID);
            if (playerData == null && shouldCreate)
            {
                playerData = new Th3PlayerData(playerUID);
                Add(playerData);
            }
            return playerData;
        }

        internal void GameWorldSave(ICoreServerAPI api)
        {
            foreach (Th3PlayerData playerData in Players)
            {
                if (playerData.IsDirty)
                {
                    playerData.IsDirty = false;
                    byte[] data = SerializerUtil.Serialize(playerData);
                    IPlayer player = api.World.PlayerByUid(playerData.PlayerUID);
                    player.WorldData.SetModdata(Th3Essentials.Th3EssentialsModDataKey, data);
                }
            }
        }

        public void Add(Th3PlayerData playerData)
        {
            Players.Add(playerData);
        }
    }
}