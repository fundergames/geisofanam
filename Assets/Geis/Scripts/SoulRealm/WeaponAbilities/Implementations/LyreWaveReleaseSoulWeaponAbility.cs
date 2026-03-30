using System.Collections.Generic;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Lyre Sword: spends Lyre resonance to apply weapon effects in a forward capsule.
    /// Assign a dedicated CombatAction for the wave, or leave null to use the current weapon definition action.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Lyre_WaveRelease",
        menuName = "Geis/Soul Realm/Lyre Sword/Wave Release")]
    public sealed class LyreWaveReleaseSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [SerializeField] private float resonanceCost = 40f;
        [SerializeField] private float waveDistance = 8f;
        [SerializeField] private float waveRadius = 1.25f;
        [Tooltip("Optional. If null, uses GeisWeaponDefinition.GetCombatAction() for the activating weapon.")]
        [SerializeField] private CombatAction waveCombatAction;
        [SerializeField] private LayerMask enemyLayers = ~0;

        public override string AbilityDisplayName => "Wave Release";

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            if (context.Owner == null)
                return;

            var meter = context.Owner.GetComponentInParent<LyreResonanceMeter>();
            if (meter == null || meter.Current < resonanceCost)
                return;

            var executor = context.Owner.GetComponentInParent<CombatExecutor>();
            var attacker = context.Owner.GetComponentInParent<CombatEntity>();
            if (executor == null || attacker == null)
                return;

            CombatAction action = waveCombatAction;
            if (action == null && context.WeaponDefinition != null)
                action = context.WeaponDefinition.GetCombatAction();
            if (action == null || action.effects == null || action.effects.Length == 0)
                return;

            Vector3 origin = context.OriginWorld + Vector3.up * 0.5f;
            Vector3 dir = context.ForwardWorld;
            if (dir.sqrMagnitude < 1e-4f)
                dir = Vector3.forward;
            dir.Normalize();

            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                waveRadius,
                dir,
                waveDistance,
                enemyLayers,
                QueryTriggerInteraction.Collide);

            var seen = new HashSet<CombatEntity>();
            var targets = new List<CombatEntity>();

            for (var i = 0; i < hits.Length; i++)
            {
                var ce = hits[i].collider.GetComponentInParent<CombatEntity>();
                if (ce == null || ce == attacker || !seen.Add(ce))
                    continue;
                var td = ce.GetEntityData();
                if (td == null || !td.IsAlive)
                    continue;
                targets.Add(ce);
            }

            if (targets.Count == 0)
                return;

            if (!meter.TryConsume(resonanceCost))
                return;

            var entityData = attacker.GetEntityData();
            if (entityData != null && context.WeaponDefinition != null)
                entityData.equippedWeapon = context.WeaponDefinition.GetWeaponForDamage();

            executor.ApplyActionToTargets(action, targets);
        }
    }
}
