using System;
using System.Collections.Generic;
using System.Text;

namespace Th3Essentials.Config;

public class Th3Config
{
    public bool IsDirty;

    public Th3DiscordConfig? DiscordConfig;

    public string? InfoMessage;

    public List<string>? AnnouncementMessages;

    public int AnnouncementInterval;
    public int AnnouncementChatGroupUid;

    public int HomeLimit = -1;

    public int HomeCooldown = 60;
    public int BackCooldown = 60;
    public bool ExcludeHomeFromBack;
    public bool ExcludeBackFromBack;
    public StarterkitItem? HomeItem;
    public StarterkitItem? SetHomeItem;

    public bool SpawnEnabled;

    public bool BackEnabled;

    public bool MessageEnabled;

    public List<StarterkitItem>? Items;

    public bool ShutdownEnabled;

    public bool BackupOnShutdown = false;

    public TimeSpan ShutdownTime = TimeSpan.Zero; // "00:00:00" in Th3Config.json
    public TimeSpan[]? ShutdownTimes;

    public int[]? ShutdownAnnounce;

    public string MessageCmdColor = "ff9102";
        
    public string SystemMsgColor = "ff9102";

    public bool ShowRole;
    public List<string>? ShowRoles;

    public string RoleFormat = "<font size=\"18\" color=\"{0}\"><strong>[{1}]</strong></font>{2}";

    public List<string>? AdminRoles;

    public bool WarpEnabled;

    public int WarpCooldown = -1;
    
    public StarterkitItem? WarpItem;
    
    public List<HomePoint>? WarpLocations;

    public string? ChatTimestampFormat;

    public bool EnableSmite;
        
    public int RandomTeleportRadius;
        
    public int RandomTeleportCooldown = 60;

    public StarterkitItem? RandomTeleportItem;
        
    public int TeleportToPlayerCooldown = 60;
    public bool TeleportToPlayerEnabled;
    public StarterkitItem? TeleportToPlayerItem;

    public Dictionary<string, RoleConfig>? RoleConfig;

    public void Init()
    {
        var sb = new StringBuilder();
        _ = sb.AppendLine("--------------------");
        _ = sb.AppendLine("<a href=\"https://discord.gg/\">Discord</a>");
        _ = sb.AppendLine("<strong>Important Commands:</strong>");
        _ = sb.AppendLine(".clients or .online | Shows you all online players");
        _ = sb.AppendLine("/spawn | Teleport back to the spawn");
        _ = sb.AppendLine("/back | Teleport back to last position (home/spawn teleport and death)");
        _ = sb.AppendLine("/home | List all homepoints");
        _ = sb.AppendLine("/home [name] | Teleport to a homepoint");
        _ = sb.AppendLine("/sethome [name] | Set a homepoint");
        _ = sb.AppendLine("/delhome [name] | Delete a homepoint");
        _ = sb.AppendLine("/restart | Shows time till next restart");
        _ = sb.AppendLine("/msg [Name] [Message] | Send a message to a player that is online");
        _ = sb.AppendLine("/starterkit | Recive a one time starterkit");
        _ = sb.AppendLine("/serverinfo | Show this information");
        _ = sb.AppendLine("--------------------");
        InfoMessage = sb.ToString();

        DiscordConfig = new Th3DiscordConfig();
    }

    internal double GetAnnouncementInterval()
    {
        return 1000 * 60 * AnnouncementInterval;
    }

    internal bool IsDiscordConfigured()
    {
        return DiscordConfig != null &&
               DiscordConfig.Token?.Length > 0;
    }

    internal bool IsShutdownConfigured()
    {
        return ShutdownAnnounce?.Length > 0 || ShutdownEnabled;
    }

    public HomePoint? FindWarpByName(string name)
    {
        return WarpLocations?.Find(point => point.Name == name);
    }

    public void MarkDirty()
    {
        if (!IsDirty)
        {
            IsDirty = true;
        }
    }

    internal void Reload(Th3Config configTemp)
    {
        AnnouncementInterval = configTemp.AnnouncementInterval;
        AnnouncementMessages = configTemp.AnnouncementMessages;
        InfoMessage = configTemp.InfoMessage;
        Items = configTemp.Items;

        HomeCooldown = configTemp.HomeCooldown;
        HomeLimit = configTemp.HomeLimit;
        SetHomeItem = configTemp.SetHomeItem;
        HomeItem = configTemp.HomeItem;
        BackCooldown = configTemp.BackCooldown;
        ExcludeHomeFromBack = configTemp.ExcludeHomeFromBack;
        ExcludeBackFromBack = configTemp.ExcludeBackFromBack;

        SpawnEnabled = configTemp.SpawnEnabled;
        BackEnabled = configTemp.BackEnabled;

        ShutdownEnabled = configTemp.ShutdownEnabled;
        ShutdownAnnounce = configTemp.ShutdownAnnounce;
        ShutdownTime = configTemp.ShutdownTime;
        ShutdownTimes = configTemp.ShutdownTimes;

        MessageCmdColor = configTemp.MessageCmdColor;
        MessageEnabled = configTemp.MessageEnabled;
            
        SystemMsgColor = configTemp.SystemMsgColor;

        ShowRole = configTemp.ShowRole;
        ShowRoles = configTemp.ShowRoles;
        RoleFormat = configTemp.RoleFormat;
        AdminRoles = configTemp.AdminRoles;

        WarpEnabled = configTemp.WarpEnabled;
        WarpCooldown = configTemp.WarpCooldown;
        WarpItem = configTemp.WarpItem;
        WarpLocations = configTemp.WarpLocations;
        ChatTimestampFormat = configTemp.ChatTimestampFormat;
        EnableSmite = configTemp.EnableSmite;
            
        RandomTeleportRadius = configTemp.RandomTeleportRadius;
        RandomTeleportCooldown = configTemp.RandomTeleportCooldown;
        RandomTeleportItem = configTemp.RandomTeleportItem;
            
        TeleportToPlayerCooldown = configTemp.TeleportToPlayerCooldown;
        TeleportToPlayerEnabled = configTemp.TeleportToPlayerEnabled;
        TeleportToPlayerItem = configTemp.TeleportToPlayerItem;

        RoleConfig = configTemp.RoleConfig;

        if (configTemp.DiscordConfig != null)
        {
            DiscordConfig ??= new Th3DiscordConfig();

            DiscordConfig.DiscordChatColor = configTemp.DiscordConfig.DiscordChatColor;
            DiscordConfig.UseEphermalCmdResponse = configTemp.DiscordConfig.UseEphermalCmdResponse;
            DiscordConfig.Token = configTemp.DiscordConfig.Token;
            DiscordConfig.ChannelId = configTemp.DiscordConfig.ChannelId;
            DiscordConfig.GuildId = configTemp.DiscordConfig.GuildId;
            DiscordConfig.ModerationRoles = configTemp.DiscordConfig.ModerationRoles;
            DiscordConfig.LinkedAccounts = configTemp.DiscordConfig.LinkedAccounts;
            DiscordConfig.RoleRewardsFormat = configTemp.DiscordConfig.RoleRewardsFormat;
            DiscordConfig.RewardIdToName = configTemp.DiscordConfig.RewardIdToName;
            DiscordConfig.Rewards = configTemp.DiscordConfig.Rewards;
            DiscordConfig.DiscordChatRelay = configTemp.DiscordConfig.DiscordChatRelay;
            DiscordConfig.AdminLogChannelId = configTemp.DiscordConfig.AdminLogChannelId;
            DiscordConfig.AdminPrivilegeToMonitor = configTemp.DiscordConfig.AdminPrivilegeToMonitor;
        }
    }
}