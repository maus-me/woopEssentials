namespace WoopEssentials.Config;

public class RoleConfig
{
    public int HomeLimit;
    
    public int HomeTeleportCost = -1;
    
    public int BackTeleportCost = -1;
    
    public bool WarpEnabled = true;
    
    public int WarpCost = -1;
    
    public int SetHomeCost = -1;
    
    public int RandomTeleportCost = -1;

    public bool RtpEnabled = true;

    public int TeleportToPlayerCost = -1;
    
    public bool TeleportToPlayerEnabled = true;

    public RoleConfig()
    {
    }

    public RoleConfig(int homelimit, int homeCost, int backCost, int setHomeCost, int rtpCost, int teleportToPlayerCost,
        bool rtpEnabled, bool teleportToPlayerEnabled, bool warpEnabled, int warpCost)
    {
        HomeLimit = homelimit;
        HomeTeleportCost = homeCost;
        BackTeleportCost = backCost;
        WarpEnabled = warpEnabled;
        WarpCost = warpCost;
        SetHomeCost = setHomeCost;
        RandomTeleportCost = rtpCost;
        TeleportToPlayerCost = teleportToPlayerCost;
        RtpEnabled = rtpEnabled;
        TeleportToPlayerEnabled = teleportToPlayerEnabled;
    }
}