using System.Collections.Generic;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;
using UnityEngine;
using UnityEngine.Serialization;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Lyre (Soul Realm): radial pulse from the player destroys <see cref="ISoulRealmDestroyable"/> props
    /// and can briefly reveal <see cref="SoulPathRevealElement"/> hints in range.
    /// Optionally spends Lyre resonance and/or applies combat to enemies (tuning).
    /// </summary>
    [CreateAssetMenu(
        fileName = "SoulAbility_Lyre_WaveRelease",
        menuName = "Geis/Soul Realm/Lyre Sword/Wave Release")]
    public sealed class LyreWaveReleaseSoulWeaponAbility : SoulWeaponAbilityAsset
    {
        [Header("Pulse shape")]
        [FormerlySerializedAs("waveDistance")]
        [SerializeField] private float pulseRadius = 8f;
        [FormerlySerializedAs("waveLayers")]
        [SerializeField] private LayerMask pulseLayers = ~0;

        [Header("Hidden paths / hints")]
        [Tooltip("Pulse reveals SoulPathRevealElement instances (same as Harp Path Reveal).")]
        [SerializeField] private bool revealHiddenPathElements = true;
        [Tooltip("How long revealed hints stay visible after the pulse.")]
        [SerializeField] private float revealDurationSeconds = 4f;
        [Tooltip("If true, every SoulPathRevealElement in the loaded scene is revealed (ignores radius).")]
        [SerializeField] private bool revealEntireScene;

        [Header("Soul objects")]
        [Tooltip("Destroy ISoulRealmDestroyable inside the pulse.")]
        [SerializeField] private bool destroySoulObjects = true;

        [Header("Optional: resonance")]
        [SerializeField] private bool requireResonance;
        [SerializeField] private float resonanceCost = 40f;

        [Header("Optional: enemies")]
        [SerializeField] private bool applyCombatActionToEnemies;
        [Tooltip("Optional. If null, uses GeisWeaponDefinition.GetCombatAction() for the activating weapon.")]
        [SerializeField] private CombatAction waveCombatAction;
        [SerializeField] private LayerMask enemyLayers = ~0;

        [Header("Puzzles")]
        [Tooltip(
            "Also notify IPuzzleMeleeHitSink (e.g. SwordHitTrigger) inside the pulse. Required in Soul Realm because normal melee does not run SimpleAttackHitDetector.")]
        [SerializeField] private bool notifySwordPuzzleVolumes = true;

        public override string AbilityDisplayName => "Wave Release";

        public override bool AllowActivationInSoulRealm => true;

        public override bool AllowActivationInPhysicalRealm => false;

        public override void Activate(in SoulWeaponAbilityContext context)
        {
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                return;

            if (context.Owner == null)
                return;

            var meter = context.Owner.GetComponentInParent<LyreResonanceMeter>();
            if (requireResonance && (meter == null || !meter.TryConsume(resonanceCost)))
                return;

            PlayDefaultActivationVfx(context);

            Vector3 origin = context.OriginWorld + Vector3.up * 0.5f;

            if (revealHiddenPathElements && revealEntireScene)
            {
                var all = Object.FindObjectsByType<SoulPathRevealElement>(FindObjectsSortMode.None);
                for (var i = 0; i < all.Length; i++)
                {
                    if (all[i] != null)
                        all[i].RevealTemporary(revealDurationSeconds);
                }
            }

            Collider[] hits = Physics.OverlapSphere(origin, pulseRadius, pulseLayers, QueryTriggerInteraction.Collide);

            if (revealHiddenPathElements && !revealEntireScene)
            {
                var revealSeen = new HashSet<int>();
                for (var i = 0; i < hits.Length; i++)
                {
                    var reveal = hits[i].GetComponentInParent<SoulPathRevealElement>();
                    if (reveal == null)
                        continue;
                    int rid = reveal.gameObject.GetInstanceID();
                    if (!revealSeen.Add(rid))
                        continue;
                    reveal.RevealTemporary(revealDurationSeconds);
                }
            }

            if (destroySoulObjects)
            {
                var soulSeen = new HashSet<int>();
                for (var i = 0; i < hits.Length; i++)
                {
                    var col = hits[i];
                    if (col == null)
                        continue;
                    int id = col.gameObject.GetInstanceID();
                    if (!soulSeen.Add(id))
                        continue;

                    var destroyable = col.GetComponentInParent<ISoulRealmDestroyable>();
                    destroyable?.DestroyFromSoulWave();
                }
            }

            if (notifySwordPuzzleVolumes)
            {
                CombatAction puzzleAction = waveCombatAction;
                if (puzzleAction == null && context.WeaponDefinition != null)
                    puzzleAction = context.WeaponDefinition.GetCombatAction();

                var puzzleSeen = new HashSet<IPuzzleMeleeHitSink>();
                for (var i = 0; i < hits.Length; i++)
                {
                    var col = hits[i];
                    if (col == null)
                        continue;
                    var sink = col.GetComponentInParent<IPuzzleMeleeHitSink>();
                    if (sink == null || !puzzleSeen.Add(sink))
                        continue;
                    sink.OnMeleeHitFromSimpleAttack(null, puzzleAction, context.WeaponSlotIndex, 1);
                }
            }

            var executor = context.Owner.GetComponentInParent<CombatExecutor>();
            var attacker = context.Owner.GetComponentInParent<CombatEntity>();

            if (applyCombatActionToEnemies && executor != null && attacker != null)
            {
                CombatAction action = waveCombatAction;
                if (action == null && context.WeaponDefinition != null)
                    action = context.WeaponDefinition.GetCombatAction();
                if (action != null && action.effects != null && action.effects.Length > 0)
                {
                    var seenEnemies = new HashSet<CombatEntity>();
                    var targets = new List<CombatEntity>();

                    for (var i = 0; i < hits.Length; i++)
                    {
                        var ce = hits[i].GetComponentInParent<CombatEntity>();
                        if (ce == null || ce == attacker || !seenEnemies.Add(ce))
                            continue;
                        var td = ce.GetEntityData();
                        if (td == null || !td.IsAlive)
                            continue;
                        if (((1 << hits[i].gameObject.layer) & enemyLayers.value) == 0)
                            continue;
                        targets.Add(ce);
                    }

                    if (targets.Count > 0)
                    {
                        var entityData = attacker.GetEntityData();
                        if (entityData != null && context.WeaponDefinition != null)
                            entityData.equippedWeapon = context.WeaponDefinition.GetWeaponForDamage();

                        executor.ApplyActionToTargets(action, targets);
                    }
                }
            }
        }
    }
}
