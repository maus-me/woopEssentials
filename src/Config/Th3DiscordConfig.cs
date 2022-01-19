using System.Collections.Generic;

namespace Th3Essentials.Config
{
    public class Th3DiscordConfig
    {
        public string Token = null;

        public ulong ChannelId = 0;

        public ulong GuildId = 0;

        public bool UseEphermalCmdResponse = true;

        public string DiscordChatColor = "7289DA";

        public List<ulong> ModerationRoles = null;

        public ulong HelpRoleID = 0;
    }
}