using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Th3Essentials
{
    public static class Th3Util
    {
        public static string GetVsVersion()
        {
            var fieldInfo = typeof(GameVersion).GetField(nameof(GameVersion.OverallVersion), BindingFlags.Public | BindingFlags.Static);
            if (fieldInfo == null)
            {
                throw new Exception("Cannot read 'GameVersion.OverallVersion'");
            }
            return fieldInfo.GetValue(null) as string;
        }
        
        public static TimeSpan GetTimeTillRestart(TimeSpan time, bool nextDayOnNegative = false)
        {
            var now = DateTime.Now;
            var restartDate = new DateTime(now.Year, now.Month, now.Day, time.Hours, time.Minutes, time.Seconds);

            if (nextDayOnNegative && now.TimeOfDay > time)
            {
                restartDate = restartDate.AddDays(1);
            }
            return restartDate - now;
        }

        public static string GetAdmins(ICoreServerAPI sapi)
        {
            List<string> admins = Th3Essentials.Config.AdminRoles;

            if (!(admins?.Count > 0))
            {
                return "There are no admin roles configured";
            }

            Dictionary<string, List<string>> online = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> offline = new Dictionary<string, List<string>>();

            foreach (string adminRole in admins)
            {
                online.Add(adminRole, new List<string>());
                offline.Add(adminRole, new List<string>());
            }

            foreach (KeyValuePair<string, ServerPlayerData> player in ((PlayerDataManager)sapi.PlayerData).PlayerDataByUid)
            {
                if (admins.Any((role) => role.ToLower().Equals(player.Value.RoleCode.ToLower())))
                {
                    if (sapi.World.AllOnlinePlayers.Any((pl) => pl.PlayerUID.Equals(player.Value.PlayerUID)))
                    {
                        online[player.Value.RoleCode.ToLower()].Add(player.Value.LastKnownPlayername);
                    }
                    else
                    {
                        offline[player.Value.RoleCode.ToLower()].Add(player.Value.LastKnownPlayername);
                    }
                }
            }
            foreach (string adminRole in admins)
            {
                online[adminRole].Sort();
                offline[adminRole].Sort();
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Online:");
            foreach (string adminRole in admins)
            {
                if (online[adminRole]?.Count > 0)
                {
                    sb.AppendLine();
                    sb.Append($"   Role: {adminRole}");
                    sb.AppendLine();
                    sb.Append("    ");
                    sb.Append(string.Join(", ", online[adminRole]));
                }
            }
            sb.AppendLine();
            sb.Append("Offline:");
            foreach (string adminRole in admins)
            {
                if (offline[adminRole]?.Count > 0)
                {
                    sb.AppendLine();
                    sb.Append($"   Role: {adminRole}");
                    sb.AppendLine();
                    sb.Append("    ");
                    sb.Append(string.Join(", ", offline[adminRole]));
                }
            }
            return sb.ToString();
        }
    }
}