using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    public abstract class CommandsLoader
    {
        internal static void Init(ICoreServerAPI sapi)
        {
            new Serverinfo().Init(sapi);
            new Message().Init(sapi);
            new Restart().Init(sapi);
            new Warp().Init(sapi);
        }
    }
}