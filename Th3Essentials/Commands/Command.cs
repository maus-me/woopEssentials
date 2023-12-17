using Vintagestory.API.Server;

namespace Th3Essentials.Commands;

internal abstract class Command
{
    internal abstract void Init(ICoreServerAPI api);
}