using Vintagestory.API.Server;

namespace CBSEssentials.Commands
{
    internal abstract class Command
    {
        internal abstract void Init(ICoreServerAPI api);
    }
}