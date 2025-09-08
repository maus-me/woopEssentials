using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Systems
{
    // Behavior to toggle whether this player can deal/receive PvP damage via /pvp commands
    public class EntityBehaviorPvp : EntityBehavior
    {
        private const string AttrKey = "th3essentials-pvp-enabled";

        // Simple anti-spam: don't notify the same player more often than every 3 seconds per key
        private static readonly Dictionary<string, DateTime> LastNotify = new();
        private static readonly TimeSpan NotifyCooldown = TimeSpan.FromSeconds(3);

        private static void NotifyOnce(IServerPlayer? sp, string key, string message)
        {
            if (sp == null) return;
            var uid = sp.PlayerUID ?? "";
            var fullKey = uid + ":" + key;
            if (LastNotify.TryGetValue(fullKey, out var last) && DateTime.UtcNow - last < NotifyCooldown) return;
            LastNotify[fullKey] = DateTime.UtcNow;

            try
            {
                sp.SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
            }
            catch
            {
                // ignore any send failures
            }
        }

        public bool Enabled
        {
            get => entity?.WatchedAttributes?.GetBool(AttrKey, false) ?? false;
            set
            {
                if (entity?.WatchedAttributes == null) return;
                entity.WatchedAttributes.SetBool(AttrKey, value);
                entity.WatchedAttributes.MarkPathDirty(AttrKey);
            }
        }

        public EntityBehaviorPvp(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return nameof(EntityBehaviorPvp);
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            // Ensure attribute exists so clients know initial state
            var _ = Enabled;
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {

            // Only interested in player-vs-player interactions
            if (damage <= 0) return;
            if (damageSource == null) return;

            // If the source is a player and the victim has PvP disabled, cancel damage
            if (damageSource.Source == EnumDamageSource.Player)
            {
                // If the attacking entity is a player, also require their PvP to be enabled to deal damage
                var attacker = damageSource.GetCauseEntity() as EntityPlayer ?? damageSource.SourceEntity as EntityPlayer;
                if (!Enabled)
                {
                    damageSource.KnockbackStrength = 0;
                    damageSource.DamageTier = 0;
                    damageSource.Type = EnumDamageType.Heal;
                    damage = 0f;

                    // Notify attacker that the victim has PvP disabled
                    var attackerSp = attacker?.Player as IServerPlayer;
                    var victimPlayer = (entity as EntityPlayer)?.Player as IServerPlayer;
                    var victimName = victimPlayer?.PlayerName ?? "The player";
                    NotifyOnce(attackerSp, "pvp-victim-disabled", $"{victimName} has PvP disabled.");

                    return;
                }
                if (attacker != null)
                {
                    var attackerPvp = attacker.GetBehavior<EntityBehaviorPvp>();
                    if (attackerPvp != null && !attackerPvp.Enabled)
                    {
                        damageSource.KnockbackStrength = 0;
                        damageSource.DamageTier = 0;
                        damageSource.Type = EnumDamageType.Heal;
                        damage = 0f;

                        // Notify attacker that their PvP is disabled
                        var attackerSp = attacker.Player as IServerPlayer;
                        NotifyOnce(attackerSp, "pvp-attacker-disabled", "Your PvP is disabled, you cannot damage other players.");

                        return;
                    }
                }
            }
        }
    }
}
