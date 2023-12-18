using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Th3Essentials;

public static class Th3Util
{
    public static string GetVsVersion()
    {
        var fieldInfo = typeof(GameVersion).GetField(nameof(GameVersion.OverallVersion),
            BindingFlags.Public | BindingFlags.Static);
        var value = fieldInfo?.GetValue(null) as string;
        if (value == null)
        {
            throw new Exception("Cannot read 'GameVersion.OverallVersion'");
        }

        return value;
    }

    public static DateTime GetRestartDate(TimeSpan time, DateTime now)
    {
        var restartDate = new DateTime(now.Year, now.Month, now.Day, time.Hours, time.Minutes, time.Seconds);

        if (now.TimeOfDay > time)
        {
            restartDate = restartDate.AddDays(1);
        }

        return restartDate;
    }

    public static string GetAdmins(ICoreServerAPI sapi)
    {
        var admins = Th3Essentials.Config.AdminRoles;

        if (!(admins?.Count > 0))
        {
            return "There are no admin roles configured";
        }

        Dictionary<string, List<string>> online = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> offline = new Dictionary<string, List<string>>();

        foreach (var adminRole in admins)
        {
            online.Add(adminRole, new List<string>());
            offline.Add(adminRole, new List<string>());
        }

        foreach (KeyValuePair<string, ServerPlayerData> player in ((PlayerDataManager)sapi.PlayerData)
                 .PlayerDataByUid)
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

        foreach (var adminRole in admins)
        {
            online[adminRole].Sort();
            offline[adminRole].Sort();
        }

        var sb = new StringBuilder();
        sb.Append("Online:");
        foreach (var adminRole in admins)
        {
            if (online[adminRole].Count > 0)
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
        foreach (var adminRole in admins)
        {
            if (offline[adminRole].Count > 0)
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

    public static string ExtractDeathMessage(IServerPlayer byPlayer, DamageSource? damageSource)
    {
        string msg;
        if (damageSource != null)
        {
            string? key = null;
            var numMax = 1;
            if (damageSource.SourceEntity != null)
            {
                key = damageSource.SourceEntity.Code.Path.Replace("-", "");
                if (key.Contains("wolf"))
                {
                    numMax = 4;
                }
                else if (key.Contains("pig"))
                {
                    numMax = 1;
                }
                else if (key.Contains("drifter"))
                {
                    numMax = 3;
                }
                else if (key.Contains("sheep"))
                {
                    if (key.Contains("female"))
                    {
                        key = "sheepbighornmale";
                    }

                    numMax = 3;
                }
                else if (key.Contains("locust"))
                {
                    numMax = 2;
                }
            }
            else
            {
                if (damageSource.Source == EnumDamageSource.Explosion)
                {
                    key = "explosion";
                    numMax = 4;
                }
                else if (damageSource.Type == EnumDamageType.Hunger)
                {
                    key = "hunger";
                    numMax = 3;
                }
                else if (damageSource.Type == EnumDamageType.Fire)
                {
                    key = "fire-block";
                    numMax = 3;
                }
                else if (damageSource.Source == EnumDamageSource.Fall)
                {
                    key = "fall";
                    numMax = 4;
                }
            }

            if (key != null)
            {
                var rnd = new Random();

                msg = Lang.Get("deathmsg-" + key + "-" + rnd.Next(1, numMax), byPlayer.PlayerName);
                if (msg.Contains("deathmsg"))
                {
                    var str = Lang.Get("prefixandcreature-" + key);
                    msg = Lang.Get("th3essentials:playerdeathby", byPlayer.PlayerName, str);
                }
            }
            else
            {
                msg = Lang.Get("th3essentials:playerdeath", byPlayer.PlayerName);
            }
        }
        else
        {
            msg = Lang.Get("th3essentials:playerdeath", byPlayer.PlayerName);
        }

        return msg;
    }
}