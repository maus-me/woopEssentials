using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace WoopEssentials;

public static class WoopUtil
{
    public static string GetVsVersion()
    {
        var fieldInfo = typeof(GameVersion).GetField(nameof(GameVersion.ShortGameVersion),
            BindingFlags.Public | BindingFlags.Static);
        var value = fieldInfo?.GetValue(null) as string;
        return value ?? throw new Exception("Cannot read 'GameVersion.ShortGameVersion'");
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
        var admins = WoopEssentials.Config.AdminRoles;

        if (!(admins?.Count > 0))
        {
            return "There are no admin roles configured";
        }

        var online = new Dictionary<string, List<string>>();
        var offline = new Dictionary<string, List<string>>();

        foreach (var adminRole in admins)
        {
            online.Add(adminRole, []);
            offline.Add(adminRole, []);
        }

        foreach (var player in ((PlayerDataManager)sapi.PlayerData)
                 .PlayerDataByUid.Where(player => admins.Any((role) => role.ToLower().Equals(player.Value.RoleCode.ToLower()))))
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

        foreach (var adminRole in admins)
        {
            online[adminRole].Sort();
            offline[adminRole].Sort();
        }

        var sb = new StringBuilder();
        sb.Append("Online:");
        foreach (var adminRole in admins.Where(adminRole => online[adminRole].Count > 0))
        {
            sb.AppendLine();
            sb.Append($"   Role: {adminRole}");
            sb.AppendLine();
            sb.Append("    ");
            sb.Append(string.Join(", ", online[adminRole]));
        }

        sb.AppendLine();
        sb.Append("Offline:");
        foreach (var adminRole in admins.Where(adminRole => offline[adminRole].Count > 0))
        {
            sb.AppendLine();
            sb.Append($"   Role: {adminRole}");
            sb.AppendLine();
            sb.Append("    ");
            sb.Append(string.Join(", ", offline[adminRole]));
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
                else switch (damageSource.Type)
                {
                    case EnumDamageType.Hunger:
                        key = "hunger";
                        numMax = 3;
                        break;
                    case EnumDamageType.Fire:
                        key = "fire-block";
                        numMax = 3;
                        break;
                    default:
                    {
                        if (damageSource.Source == EnumDamageSource.Fall)
                        {
                            key = "fall";
                            numMax = 4;
                        }

                        break;
                    }
                }
            }

            if (key != null)
            {
                var rnd = new Random();

                msg = Lang.Get("deathmsg-" + key + "-" + rnd.Next(1, numMax), byPlayer.PlayerName);
                if (!msg.Contains("deathmsg")) return msg;
                var str = Lang.Get("prefixandcreature-" + key);
                msg = Lang.Get("woopessentials:playerdeathby", byPlayer.PlayerName, str);
            }
            else
            {
                msg = Lang.Get("woopessentials:playerdeath", byPlayer.PlayerName);
            }
        }
        else
        {
            msg = Lang.Get("woopessentials:playerdeath", byPlayer.PlayerName);
        }

        return msg;
    }

    public static string PrettyTime(TimeSpan span)
    {
        var parts = new List<string>();
        if (span.Days > 0)
            parts.Add($"{span.Days}d");
        if (span.Hours > 0)
            parts.Add($"{span.Hours}h");
        if (span.Minutes > 0)
            parts.Add($"{span.Minutes}m");
        if (span.Seconds > 0 || parts.Count == 0) // Always show seconds if nothing else
            parts.Add($"{span.Seconds}s");

        return string.Join(", ", parts);
    }
}