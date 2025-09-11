using Vintagestory.API.Server;

namespace WoopEssentials.Commands;

internal abstract class Command
{
    internal abstract void Init(ICoreServerAPI api);
}