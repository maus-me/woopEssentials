using System.Collections.Generic;
using Th3Essentials.PlayerData;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Th3Essentials.Config
{
    public class Th3PlayerConfig
    {
        public bool IsDirty { get; private set; }

        private readonly List<Th3PlayerData> Players;

        public Th3PlayerConfig()
        {
            Players = new List<Th3PlayerData>();
        }

        public Th3PlayerData GetPlayerDataByUID(string playerUID)
        {
            return Players.Find(player => player.PlayerUID == playerUID);
        }

        public void MarkDirty()
        {
            if (!IsDirty)
            {
                IsDirty = true;
            }
        }

        internal void GameWorldSave(ICoreServerAPI api)
        {
            if (IsDirty)
            {
                foreach (Th3PlayerData playerData in Players)
                {
                    byte[] data = SerializerUtil.Serialize(playerData);
                    IPlayer player = api.World.PlayerByUid(playerData.PlayerUID);
                    player.WorldData.SetModdata(Th3Essentials.Th3EssentialsModDataKey, data);
                }
            }
        }

        public void Add(Th3PlayerData playerData)
        {
            Players.Add(playerData);
            MarkDirty();
        }
    }
}