
using Discord.WebSocket;

namespace WoopEssentials.Config;

public class Rewards
{
    public SocketRole SocketRole;

    public string Name;

    public Rewards(SocketRole socketRole, string name)
    {
        SocketRole = socketRole;
        Name = name;
    }

}