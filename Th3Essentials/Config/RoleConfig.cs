namespace Th3Essentials.Config;

public class RoleConfig
{
    public int HomeLimit;
    
    public int HomeTeleportCost = -1;
    
    public int BackTeleportCost = -1;
    
    public int SetHomeCost = -1;
    
    public int RandomTeleportCost = -1;

    public bool RtpEnabled = true;

    public int TeleportToPlayerCost = -1;
    
    public bool TeleportToPlayerEnabled = true;

    public RoleConfig()
    {
    }

    public RoleConfig(int homelimit, int homeCost, int backCost, int setHomeCost, int rtpCost, int teleportToPlayerCost,
        bool rtpEnabled, bool teleportToPlayerEnabled)
    {
        HomeLimit = homelimit;
        HomeTeleportCost = homeCost;
        BackTeleportCost = backCost;
        SetHomeCost = setHomeCost;
        RandomTeleportCost = rtpCost;
        TeleportToPlayerCost = teleportToPlayerCost;
        RtpEnabled = rtpEnabled;
        TeleportToPlayerEnabled = teleportToPlayerEnabled;
    }
}