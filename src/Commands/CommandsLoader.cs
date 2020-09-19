using Vintagestory.API.Server;

namespace CBSEssentials.Commands
{
    public class CommandsLoader
    {
        internal static void init(ICoreServerAPI api)
        {
            new Players().init(api);
            new Info().init(api);
        }
    }
}