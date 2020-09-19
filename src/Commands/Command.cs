using Vintagestory.API.Server;

namespace CBSEssentials.Commands
{
    internal abstract class Command
    {
        internal abstract void init(ICoreServerAPI api);
    }
}