using WoopEssentials.Discord;

namespace WoopEssentials;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Server;

// ReSharper disable InconsistentNaming
public class PatchAdminLogging
{
    public static void Patch(Harmony harmony)
    {
        var executeMethod = typeof(ChatCommandApi).GetMethods().First(m =>
            m.Name.Equals("Execute") &&
            m.GetParameters().Any(p => p.ParameterType.IsAssignableFrom(typeof(IServerPlayer))));
        var executePrefix =
            new HarmonyMethod(typeof(PatchAdminLogging).GetMethod(nameof(TriggerChatCommand)));
        harmony.Patch(executeMethod, prefix: executePrefix);

        var activateSlotMethod =
            typeof(InventoryPlayerCreative).GetMethod(nameof(InventoryPlayerCreative.ActivateSlot));
        var activateSlotPostfix =
            new HarmonyMethod(typeof(PatchAdminLogging).GetMethod(nameof(ActivateSlot)));
        harmony.Patch(activateSlotMethod, postfix: activateSlotPostfix);

        var handleCreateItemstack = typeof(ServerMain).Assembly.GetType("Vintagestory.Server.ServerSystemInventory")?
            .GetMethod("HandleCreateItemstack", BindingFlags.NonPublic | BindingFlags.Instance);
        var handleCreateItemstackPostfix =
            new HarmonyMethod(typeof(PatchAdminLogging).GetMethod(nameof(HandleCreateItemstack)));
        harmony.Patch(handleCreateItemstack, postfix: handleCreateItemstackPostfix);
    }


    [SuppressMessage("ReSharper", "UnusedParameter.Global")]
    public static void TriggerChatCommand(string commandName, IServerPlayer player, int groupId, string args,
        Action<TextCommandResult> onCommandComplete)
    {
        try
        {
            var cmd = WoopDiscord.Instance.Sapi.ChatCommands.Get(commandName) as ChatCommandImpl;
            var priv = cmd?.GetType().GetField("privilege", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(cmd);

            // if (priv is string s && player.HasPrivilege(s))
            // {
            if (WoopEssentials.Config.DiscordConfig?.AdminPrivilegeToMonitor?.Contains(priv) == true)
            {
                WoopDiscord.Instance.SendAdminLog($"**{player.PlayerName}** executed: {commandName} {args}");
            }
            else if (Equals("gamemode", commandName) || Equals("gm", commandName))
            {
                WoopDiscord.Instance.SendAdminLog(
                    $"**{player.PlayerName}** executed @ ({player.Entity.Pos.AsBlockPos}): {commandName} {args}");
            }
            // }
        }
        catch (Exception e)
        {
            WoopDiscord.Instance.Sapi.Logger.Error(e);
        }
    }

    public static void ActivateSlot(InventoryPlayerCreative __instance, int slotId, ItemSlot sourceSlot,
        ref ItemStackMoveOperation op)
    {
        if (op.MovedQuantity == 0) return;
        if (op.ShiftDown)
        {
            var itemSlot = __instance[slotId];
            WoopDiscord.Instance.SendAdminLog(
                $"**{op.ActingPlayer?.PlayerName}** spawned: {op.MovedQuantity} {itemSlot.Itemstack?.Collectible?.Code}");
        }
        else
        {
            WoopDiscord.Instance.SendAdminLog(
                $"**{op.ActingPlayer?.PlayerName}** spawned: {op.MovedQuantity} {sourceSlot.Itemstack?.Collectible?.Code}");
        }
    }

    public static void HandleCreateItemstack(Packet_Client packet, ConnectedClient client)
    {
        try
        {
            var player = (ServerPlayer)client
                .GetType()
                .GetField("Player", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(client)!;
            var createItemstack = (Packet_CreateItemstack)packet.GetType()
                .GetField("CreateItemstack", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(packet)!;
            var targetInventoryId = (string)createItemstack
                .GetType()
                .GetField("TargetInventoryId", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(createItemstack)!;
            var targetSlot = (int)createItemstack
                .GetType()
                .GetField("TargetSlot", BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(createItemstack)!;

            player.InventoryManager.GetInventory(targetInventoryId, out var inv);
            var slot = inv?[targetSlot];

            if (player.WorldData.CurrentGameMode == EnumGameMode.Creative && slot?.Itemstack != null)
            {
                WoopDiscord.Instance.SendAdminLog(
                    $"**{player.PlayerName}** spawned: 1 {slot.Itemstack?.Collectible?.Code}");
            }
        }
        catch (NullReferenceException)
        {
        }
    }
}