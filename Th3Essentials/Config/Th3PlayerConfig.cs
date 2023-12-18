using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Th3Essentials.Config;

public class Th3PlayerConfig
{
    public Dictionary<string, Th3PlayerData> Players = new();

    /// <summary>
    /// gets or creates a Th3PlayerData object with given UID
    /// </summary>
    /// <param name="playerUid"></param>
    /// <param name="shouldCreate"></param>
    /// <returns>Th3PlayerData</returns>
    public Th3PlayerData? GetPlayerDataByUid(string playerUid, bool shouldCreate)
    {
        if (!Players.TryGetValue(playerUid, out var playerData) && shouldCreate)
        {
            playerData = new Th3PlayerData();
            playerData.MarkDirty();
            Add(playerUid, playerData);
        }
        return playerData;
    }

    public Th3PlayerData GetPlayerDataByUid(string playerUid)
    {
        return GetPlayerDataByUid(playerUid, true)!;
    }

    internal void GameWorldSave(ICoreServerAPI api)
    {
        foreach (KeyValuePair<string, Th3PlayerData> playerData in Players)
        {
            if (playerData.Value.IsDirty)
            {
                playerData.Value.IsDirty = false;
                var data = SerializerUtil.Serialize(playerData.Value);
                var player = api.World.PlayerByUid(playerData.Key);
                player.WorldData.SetModdata(Th3Essentials.Th3EssentialsModDataKey, data);
            }
        }
    }

    public void Add(string playerUid, Th3PlayerData playerData)
    {
        Players.Add(playerUid, playerData);
    }
}