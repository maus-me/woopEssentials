using System.Collections.Generic;

namespace Th3Essentials.Config
{
    public class Th3DiscordConfig
    {
        public string? Token = null;

        public ulong ChannelId = 0;

        public ulong GuildId = 0;

        public bool UseEphermalCmdResponse = true;

        public string DiscordChatColor = "7289DA";

        public List<ulong>? ModerationRoles = null;

        public ulong HelpRoleID = 0;

        public Dictionary<string, string>? LinkedAccounts = null;

        public Dictionary<string, string>? RewardIdToName = null;

        public bool Rewards = false;

        public string RoleRewardsFormat = "<font size=\"18\" color=\"{0}\"><strong>[{1}]</strong></font><font size=\"18\" color=\"{2}\"><strong>[{3}]</strong></font>{4}";

        public string RewardsFormat = "<font size=\"18\" color=\"{0}\"><strong>[{1}]</strong></font>{2}";

        public bool DiscordChatRelay = true;
        
        public ulong AdminLogChannelId = 0;

        public string[]? AdminPrivilegeToMonitor = new[]
        {
            "gamemode", "pickingrange", "kick", "ban", "whitelist", "give", "controlserver", "tp", "time", "grantrevoke", "root", "commandplayer"
        };
    }
}