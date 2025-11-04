using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using WoopEssentials.Config;
using JsonObject = System.Text.Json.Nodes.JsonObject;

namespace WoopEssentials.Systems.Chat;

public class ChatFilterSystem
{
    private ICoreServerAPI _sapi = null!;

    private readonly List<CompiledRule> _rules = new();

    private static readonly HttpClient Http = new HttpClient();

    private const string FilterMessageLabel = "<strong>[ChatFilter]</strong>";

    internal void Init(ICoreServerAPI sapi)
    {
        _sapi = sapi;

        LoadRulesFromConfig();
        _sapi.Event.PlayerChat += OnPlayerChat;
    }

    private sealed class CompiledRule
    {
        public string GroupName = "";
        public ChatFilterRule Rule = null!;
        public List<Regex> Patterns = new();
    }

    private void LoadRulesFromConfig()
    {
        _rules.Clear();
        try
        {
            var cfg = WoopEssentials.Config;
            if (!cfg.EnableChatFilter)
            {
                return;
            }

            if (cfg.ChatFilterRules is not { Count: > 0 }) return;

            foreach (var (key, list) in cfg.ChatFilterRules)
            {
                var group = key;

                for (var i = 0; i < list.Count; i++)
                {
                    var rule = list[i];
                    if (!rule.enabled) continue;
                    var comp = new CompiledRule { GroupName = group, Rule = rule };

                    if (rule.Regex != null)
                    {
                        foreach (var pattern in rule.Regex.Where(p => !string.IsNullOrWhiteSpace(p)))
                        {
                            try
                            {
                                comp.Patterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
                            }
                            catch (Exception ex)
                            {
                                _sapi?.Logger.Warning($"Invalid chat filter regex '{pattern}' in group '{group}': {ex.Message}");
                            }
                        }
                    }

                    if (comp.Patterns.Count > 0)
                    {
                        _rules.Add(comp);
                    }
                }
            }
            _sapi?.Logger.Audit($"ChatFilterSystem loaded {_rules.Count} rule(s) from JSON.");
        }
        catch (Exception ex)
        {
            _sapi?.Logger.Error($"ChatFilterSystem failed to load rules: {ex}");
        }
    }

    private void OnPlayerChat(IServerPlayer byPlayer, int channelId, ref string message, ref string data, BoolRef consumed)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        // Ignore commands
        if (message.StartsWith('/') || message.StartsWith('.')) return;

        var cfg = WoopEssentials.Config;
        if (!cfg.EnableChatFilter) return;
        if (_rules.Count == 0) return;

        var text = message; // copy ref param into local for use in lambdas/loops
        foreach (var cr in _rules)
        {
            var matched = cr.Patterns.Any(rx => rx.IsMatch(text));
            if (!matched) continue;

            consumed.value = true; // prevent message from being broadcast

            // Sender feedback (unless silent)
            if (!cr.Rule.silent)
            {
                byPlayer.SendMessage(GlobalConstants.GeneralChatGroup,
                    $"{FilterMessageLabel} Your message was blocked by the server chat filter for {cr.GroupName}.",
                    EnumChatType.Notification);
            }

            var reason = cr.Rule.Reason ?? "Message matched a filtered pattern";
            var preview = message.Length > 120 ? message.Substring(0, 120) + "…" : message;

            // Log
            _sapi.Logger.Audit($"[ChatFilter] Group={cr.GroupName} Player={byPlayer.PlayerName} Msg='{preview}' Reason='{reason}'");

            // Alert staff
            if (cr.Rule.alertstaff)
            {
                try
                {
                    foreach (var p in _sapi.World.AllOnlinePlayers)
                    {
                        if (p is IServerPlayer sp && sp.HasPrivilege(Privilege.controlserver))
                        {
                            sp.SendMessage(GlobalConstants.GeneralChatGroup,
                                $"{FilterMessageLabel} Filtered {cr.GroupName} message by {byPlayer.PlayerName}: '{preview}'",
                                EnumChatType.Notification);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _sapi.Logger.Error($"[ChatFilter] Failed to alert staff: {ex}");
                }
            }

            // Discord webhook announcement
            if (cr.Rule.announcediscord && !string.IsNullOrWhiteSpace(cfg.ChatFilterDiscordWebhook))
            {
                var hook = cfg.ChatFilterDiscordWebhook!;
                _ = PostDiscordAsync(hook, byPlayer, message, cr.GroupName, reason);
            }

            // Commands as console
            if (!string.IsNullOrWhiteSpace(cr.Rule.command))
            {
                try
                {
                    var cmdArgs = new TextCommandCallingArgs
                    {
                        Caller = new Caller()
                        {
                            Type = EnumCallerType.Console,
                            CallerRole = "admin",
                            CallerPrivileges = new[] { "*" },
                            FromChatGroupId = GlobalConstants.ConsoleGroup
                        }
                    };
                    var cmd = cr.Rule.command!
                        .Replace("{sendername}", byPlayer.PlayerName)
                        .Replace("{reason}", reason)
                        .Replace("{message}", message)
                        .Replace("{group}", cr.GroupName);

                    _sapi.Logger.Audit($"[ChatFilter] Executing as console: {cmd}");
                    _sapi.ChatCommands.ExecuteUnparsed(cmd, cmdArgs);
                }
                catch (Exception ex)
                {
                    _sapi.Logger.Error($"[ChatFilter] Failed to execute command '{cr.Rule.command}': {ex}");
                }
            }

            return; // Only process the first matching rule
        }
    }

    private async Task PostDiscordAsync(string webhookUrl, IServerPlayer player, string message, string groupName, string reason)
    {
        try
        {
            // Minimal Discord-compatible JSON for webhook: send as content

            // New (full payload via Nodes)
            var payload = new JsonObject
            {
                ["username"] = "ChatFilter",
                ["avatar_url"] = "https://upload.wikimedia.org/wikipedia/en/1/1b/Vintage_Story_Logo.png",
                ["content"] = "",
                ["embeds"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["author"] = new JsonObject { ["name"] = $"" },
                        ["title"] = "",
                        ["description"] = "",
                        ["color"] = 15258703,
                        ["fields"] = new JsonArray
                        {
                            new JsonObject { ["name"] = "Username", ["value"] = $"{{player.PlayerName}}", ["inline"] = true },
                            new JsonObject { ["name"] = "Rule", ["value"] = $"{groupName}", ["inline"] = true },
                            new JsonObject { ["name"] = "Message", ["value"] = $"{message}" }
                        }
                        // },
                        // ["thumbnail"] = new JsonObject { ["url"] = "https://upload.wikimedia.org/wikipedia/commons/3/38/4-Nature-Wallpapers-2014-1_ukaavUI.jpg" },
                        // ["image"] = new JsonObject { ["url"] = "https://upload.wikimedia.org/wikipedia/commons/5/5a/A_picture_from_China_every_day_108.jpg" },
                        // ["footer"] = new JsonObject { ["text"] = "Woah! So cool! :smirk:", ["icon_url"] = "https://i.imgur.com/fKL31aD.jpg" }
                    }
                }
            };

            // content = $"Group={groupName} Player={player.PlayerName} {message}\nReason: {reason}"
            var json = JsonSerializer.Serialize(payload);
            using var strContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var resp = await Http.PostAsync(webhookUrl, strContent, cts.Token).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                _sapi.Logger.Warning($"[ChatFilter] Discord webhook returned {(int)resp.StatusCode} {resp.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[ChatFilter] Discord webhook failed: {ex.Message}");
        }
    }
}