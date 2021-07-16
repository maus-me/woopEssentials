using Vintagestory.API.Server;

namespace Th3Essentials.Commands
{
    public class CommandsLoader
    {
        internal static void Init(ICoreServerAPI api)
        {
            new Info().Init(api);
        }
    }
}