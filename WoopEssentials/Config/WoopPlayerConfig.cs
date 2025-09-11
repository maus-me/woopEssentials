using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace WoopEssentials.Config;

public class WoopPlayerConfig
{
    public Dictionary<string, WoopPlayerData> Players = new();

    /// <summary>
    /// gets or creates a WoopPlayerData object with given UID
    /// </summary>
    /// <param name="playerUid"></param>
    /// <param name="shouldCreate"></param>
    /// <returns>WoopPlayerData</returns>
    public WoopPlayerData? GetPlayerDataByUid(string playerUid, bool shouldCreate)
    {
        if (!Players.TryGetValue(playerUid, out var playerData) && shouldCreate)
        {
            playerData = new WoopPlayerData();
            playerData.MarkDirty();
            Add(playerUid, playerData);
        }
        return playerData;
    }

    public WoopPlayerData GetPlayerDataByUid(string playerUid)
    {
        return GetPlayerDataByUid(playerUid, true)!;
    }

    internal void GameWorldSave(ICoreServerAPI api)
    {
        foreach (KeyValuePair<string, WoopPlayerData> playerData in Players)
        {
            if (playerData.Value.IsDirty)
            {
                playerData.Value.IsDirty = false;
                var data = SerializerUtil.Serialize(playerData.Value);
                var player = api.World.PlayerByUid(playerData.Key);
                player.WorldData.SetModdata(WoopEssentials.WoopEssentialsModDataKey, data);
            }
        }
    }

    public void Add(string playerUid, WoopPlayerData playerData)
    {
        Players.Add(playerUid, playerData);
    }
}