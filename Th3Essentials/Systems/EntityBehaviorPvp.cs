using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Th3Essentials.Systems
{
    // Behavior to toggle whether this player can deal/receive PvP damage via /pvp commands, plus cooldown handling
    public class EntityBehaviorPvp : EntityBehavior
    {
        private const string AttrKey = "th3essentials-pvp-enabled";
        private const string CooldownAttrKey = "th3essentials-pvp-cooldown-until"; // stores UTC ticks (long)

        // Simple anti-spam: don't notify the same player more often than every 3 seconds per key
        private static readonly Dictionary<string, DateTime> LastNotify = new();
        private static readonly TimeSpan NotifyCooldown = TimeSpan.FromSeconds(3);

        // Default cooldown duration in seconds after enabling PvP
        public static int DefaultCooldownSeconds = 90;

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
            // Ensure attributes exist so clients know initial state
            var _ = Enabled;
            var __ = CooldownUntilUtcTicks; // touch to ensure existence
        }

        // Cooldown storage helpers
        private long CooldownUntilUtcTicks
        {
            get => entity?.WatchedAttributes?.GetLong(CooldownAttrKey, 0L) ?? 0L;
            set
            {
                if (entity?.WatchedAttributes == null) return;
                entity.WatchedAttributes.SetLong(CooldownAttrKey, value);
                entity.WatchedAttributes.MarkPathDirty(CooldownAttrKey);
            }
        }

        public void StartCooldown(int? seconds = null)
        {
            var secs = seconds ?? DefaultCooldownSeconds;
            var until = DateTime.UtcNow.AddSeconds(secs).Ticks;
            CooldownUntilUtcTicks = until;
        }

        public void ResetCooldownOnCombat(int? seconds = null)
        {
            // Always extend to now+secs, regardless of existing value
            StartCooldown(seconds);
        }

        public bool IsCooldownActive(out TimeSpan remaining)
        {
            var untilTicks = CooldownUntilUtcTicks;
            if (untilTicks <= 0)
            {
                remaining = TimeSpan.Zero; return false;
            }
            var until = new DateTime(untilTicks, DateTimeKind.Utc);
            var now = DateTime.UtcNow;
            if (until <= now)
            {
                remaining = TimeSpan.Zero; return false;
            }
            remaining = until - now; return true;
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            // Only interested in player-vs-player interactions
            if (damage <= 0) return;
            if (damageSource == null) return;

            if (damageSource.Source == EnumDamageSource.Player)
            {
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
                    NotifyOnce(attackerSp, "pvp-victim-disabled", $"{victimName} has PvP disabled."); // Todo: Replace message with lang string

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
                        NotifyOnce(attackerSp, "pvp-attacker-disabled", "Your PvP is disabled, you cannot damage other players."); // Todo: Replace message with lang string

                        return;
                    }

                    // At this point, both victim (this) and attacker have PvP enabled and damage would apply
                    // Reset cooldown timer for both players as per requirement
                    attackerPvp?.ResetCooldownOnCombat();
                    this.ResetCooldownOnCombat();

                    // Notify involved players they are tagged in combat (anti-spam protected)
                    try
                    {
                        var victimSp = (entity as EntityPlayer)?.Player as IServerPlayer;
                        var attackerSp = attacker.Player as IServerPlayer;

                        // Notify victim
                        if (victimSp != null)
                        {
                            // Show a simple message, include seconds left if already active
                            if (IsCooldownActive(out var remainingVictim) && remainingVictim.TotalSeconds > 0)
                            {
                                var secs = Math.Ceiling(remainingVictim.TotalSeconds);
                                NotifyOnce(victimSp, "pvp-tagged", Lang.Get("th3essentials:pvp-tagged", secs));
                            }
                            else
                            {
                                NotifyOnce(victimSp, "pvp-tagged", Lang.Get("th3essentials:pvp-tagged", (double)DefaultCooldownSeconds));
                            }
                        }

                        // Notify attacker
                        if (attackerSp != null)
                        {
                            var secsAtt = attackerPvp != null && attackerPvp.IsCooldownActive(out var remainingAtk)
                                ? Math.Ceiling(remainingAtk.TotalSeconds)
                                : (double)DefaultCooldownSeconds;
                            NotifyOnce(attackerSp, "pvp-tagged", Lang.Get("th3essentials:pvp-tagged", secsAtt));
                        }
                    }
                    catch
                    {
                        // ignore notification issues
                    }
                }
            }
        }

        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            // If a player disconnects while under PvP combat cooldown, treat as death so items drop
            if (despawn == null) return;
            if (despawn.Reason != EnumDespawnReason.Disconnect) return;

            // Only relevant for players
            if (entity is not EntityPlayer ep) return;

            if (IsCooldownActive(out _))
            {
                despawn.Reason = EnumDespawnReason.Death;

                // Provide a death damage source if missing, to aid logging/messages
                if (despawn.DamageSourceForDeath == null)
                {
                    despawn.DamageSourceForDeath = new DamageSource()
                    {
                        Source = EnumDamageSource.Suicide,
                        Type = EnumDamageType.Injury,
                        SourceEntity = ep
                    };
                }

                try
                {
                    // Play a sound at the disconnect location via world API so it still plays even as the entity despawns
                    if (entity.Api is ICoreServerAPI sapiLocal)
                    {
                        var pos = entity.ServerPos;
                        // Using vanilla player death sound. World-level positional play avoids entity lifecycle issues.
                        sapiLocal.World.PlaySoundAt(new AssetLocation("game","sounds/player/death1"), pos.X, pos.Y, pos.Z, null, true, 24f);
                    }
                }
                catch
                {
                    // ignore sound failures
                }

                // Force death
                ep.Die(EnumDespawnReason.Death, despawn.DamageSourceForDeath);

                // Broadcast to all players that this player logged out during combat
                var sapi = entity.Api as ICoreServerAPI;
                var sp = ep.Player as IServerPlayer;
                var pname = sp?.PlayerName ?? Lang.Get("th3essentials:combat-die-reason");
                try
                {
                    sapi?.SendMessageToGroup(GlobalConstants.GeneralChatGroup,
                        Lang.Get("th3essentials:combat-logout-broadcast", pname),
                        EnumChatType.OthersMessage);
                }
                catch
                {
                    // ignore broadcast failures
                }
            }
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);

            // Only on server, only for players
            if (entity?.Api?.Side != EnumAppSide.Server) return;
            if (entity is not EntityPlayer ep) return;

            try
            {
                // Clear any active PvP cooldown on death
                CooldownUntilUtcTicks = 0L;
            }
            catch
            {
                // Swallow any error to avoid impacting death process
            }
        }
    }
}
