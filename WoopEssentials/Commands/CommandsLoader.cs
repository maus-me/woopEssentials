using Vintagestory.API.Server;

namespace WoopEssentials.Commands;

public abstract class CommandsLoader
{
    internal static void Init(ICoreServerAPI sapi)
    {
        // Initialize shared systems/helpers
        new Serverinfo().Init(sapi);
        new Message().Init(sapi);
        new Restart().Init(sapi);
        new Warp().Init(sapi);
        new Smite().Init(sapi);
        new PvP().Init(sapi);
        new RandomTeleport().Init(sapi);
        new TeleportRequest().Init(sapi);
        new WoopConfigCommands().Init(sapi);
        new HealFeed().Init(sapi);
        new PlayerStats().Init(sapi);
        new Ping().Init(sapi);
        new Afk().Init(sapi);
        new AntiGrief().Init(sapi);
    }
}