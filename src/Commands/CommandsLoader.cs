using Vintagestory.API.Server;

namespace CBSEssentials.Commands
{
    public class CommandsLoader
    {
        internal static void Init(ICoreServerAPI api)
        {
            new Players().Init(api);
            new Info().Init(api);
        }
    }
}