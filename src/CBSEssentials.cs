using CBSEssentials.Commands;
using CBSEssentials.Homepoints;
using CBSEssentials.Starterkit;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

[assembly: ModInfo("CBSEssentials",
    Description = "Chill build survival essentials mod",
    Website = "https://github.com/Th3Dilli/",
    Authors = new[] { "Th3Dilli" })]
namespace CBSEssentials
{
    public class CBSEssentials : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            CommandsLoader.init(api);
            new Homesystem().init(api);
            new Starterkitsystem().init(api);
        }
    }
}
