using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace WoopEssentials.Systems.EntityBehavior;

/// <summary>
/// Prevents damage to AFK players. Lightweight behavior attached to player entities.
/// </summary>
internal class EntityBehaviorAfkGuard(Entity entity) : Vintagestory.API.Common.Entities.EntityBehavior(entity)
{
    private const string BehaviorCode = "EntityBehaviorAfkGuard";

    public override string PropertyName()
    {
        return BehaviorCode;
    }

    public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
        try
        {
            if (damage <= 0 ||
                entity is not EntityPlayer ep ||
                ep.PlayerUID == null ||
                !(entity.World is IServerWorldAccessor) ||
                !AfkSystem.Instance.IsAfk(ep.PlayerUID)
                ) return;

            // Nullify incoming damage while AFK
            damageSource.KnockbackStrength = 0;
            damageSource.DamageTier = 0;
            damageSource.Type = EnumDamageType.Heal;
            damage = 0f;
        }
        catch
        {
            // best-effort; don't throw in damage pipeline
        }
    }
}
