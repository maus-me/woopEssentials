using Vintagestory.API.Server;
using Vintagestory.API.Common;
using System.Timers;
using System.IO;
using Vintagestory.API.Config;

namespace CBSEssentials.Announcements
{
    class Announcementsystem : ModSystem
    {
        private ICoreServerAPI _api;
        private AnnouncementsConfig config;
        private int currentMsg;

        public Announcementsystem()
        {
            currentMsg = 0;
        }

        private const string configFile = "announcements.json";

        public void init(ICoreServerAPI api)
        {
            _api = api;
            config = _api.LoadModConfig<AnnouncementsConfig>(configFile);

            if (config == null)
            {
                config = new AnnouncementsConfig();
                config.init();
                _api.StoreModConfig(config, configFile);
                _api.Server.LogWarning("Announcementsystem initialized with default config!!!");
                _api.Server.LogWarning("Announcementsystem config file at " + Path.Combine(GamePaths.ModConfig, configFile));
            }

            if (config.messages.Count != 0)
            {
                Timer announcer = new Timer(config.getInterval());
                announcer.Elapsed += announceMsg;
                announcer.AutoReset = true;
                announcer.Enabled = true;
            }
        }

        private void announceMsg(object source, ElapsedEventArgs args) //here is where the magic happens
        {
            if (currentMsg >= config.messages.Count)
            {
                currentMsg = 0;
            }

            _api.BroadcastMessageToAllGroups($"<strong>[Info]</strong> {config.messages[currentMsg]}", EnumChatType.Notification);
            currentMsg++;
        }
    }
}