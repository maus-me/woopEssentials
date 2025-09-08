using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

public abstract class CommandsLoader
{
    internal static void Init(ICoreServerAPI sapi)
    {
        new Serverinfo().Init(sapi);
        new Message().Init(sapi);
        new Restart().Init(sapi);
        new Warp().Init(sapi);
        new Smite().Init(sapi);
        new PvP().Init(sapi);
        new RandomTeleport().Init(sapi);
        new TeleportRequest().Init(sapi);
        new Th3ConfigCommands().Init(sapi);
    }
}