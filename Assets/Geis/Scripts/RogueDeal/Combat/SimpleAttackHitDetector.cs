/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Simple hit detection using OverlapSphere at delay(s) after attack.
    /// No animation events or weapon colliders needed.
    /// For <see cref="CombatAction.isCombo"/> with <see cref="CombatAction.comboHitCount"/> &gt; 1, runs multiple checks at configured times.
    /// Add to the player and set useWeaponColliders=false on the combat controller.
    /// <see cref="CombatExecutor"/> and <see cref="CombatEntity"/> are optional on the same GameObject;
    /// assign them or add those components for hit application and self-filtering. Without them, hit checks no-op.
    /// </summary>
    public class SimpleAttackHitDetector : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Seconds after attack starts before the first hit check (tune to match swing animation)")]
        [SerializeField] private float hitDelay = 0.25f;

        [Tooltip("When the action has multiple hits and Hit Timings From Attack Start is empty: spacing between checks after the first (seconds).")]
        [SerializeField] private float spacingBetweenHits = 0.15f;

        [Tooltip("Optional: absolute times from attack start for each hit (seconds). First element = first check. If empty, uses Hit Delay + Spacing Between Hits.")]
        [SerializeField] private float[] hitTimingsFromAttackStart;

        [Header("Detection")]
        [Tooltip("Center offset in front of character (meters)")]
        [SerializeField] private float rangeOffset = 1.5f;
        [Tooltip("Radius of the OverlapSphere")]
        [SerializeField] private float hitRadius = 2f;
        [Tooltip("Also check sphere at player position (catches enemies that moved)")]
        [SerializeField] private bool usePlayerCenterFallback = true;

        [Tooltip(
            "How OverlapSphere treats trigger volumes for combat hits. Ignore keeps large lock-on shells (GeisObjectLockOn) from extending melee reach; use Collide only if targets rely on trigger hurtboxes.")]
        [SerializeField] private QueryTriggerInteraction combatOverlapTriggerInteraction = QueryTriggerInteraction.Ignore;

        [Tooltip("Log hit checks and target count (for debugging)")]
        [SerializeField] private bool debugLog = false;

        /// <summary>
        /// Optional. When set (e.g. from Geis combat bridge), overlap probes use this position and planar forward
        /// instead of this transform — used for soul-realm ghost melee while the CombatEntity root stays on the body.
        /// </summary>
        public Func<(Vector3 origin, Vector3 planarForward)> OverrideMeleeProbeOrigin;
        [Tooltip("Layers to check for enemies. Use Everything (~0) unless you know all hurtboxes share one layer. A mask like 64 = only layer 6 — Default (0) is excluded.")]
        [SerializeField] private LayerMask targetLayers = ~0;
        [Tooltip("Tags that identify valid targets (empty = any)")]
        [SerializeField] private string[] validTargetTags = { "Enemy" };

        [Header("Puzzle (Geis)")]
        [Tooltip("When a weapon slot index is passed (e.g. from GeisCombatBridge), also notify IPuzzleMeleeHitSink zones overlapping the same melee probe spheres.")]
        [SerializeField] private bool notifySwordPuzzleTriggers = true;

        [Tooltip("Layers included when probing puzzle zones (Default + environment; exclude if needed).")]
        [SerializeField] private LayerMask puzzleProbeLayers = ~0;

        private CombatExecutor _executor;
        private CombatEntity _combatEntity;
        private int _hitSequenceId;

        private void Awake()
        {
            _executor = GetComponent<CombatExecutor>() ?? GetComponentInParent<CombatExecutor>();
            _combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
        }

        /// <summary>Invalidates any in-flight hit-check coroutines from a prior swing.</summary>
        public void CancelPendingHitChecks()
        {
            _hitSequenceId++;
        }

        /// <summary>
        /// Call this when an attack starts. Performs one or more hit checks based on action combo data and timing fields.
        /// </summary>
        public void PerformHitCheck(CombatAction action)
        {
            PerformHitCheck(action, null, -1);
        }

        /// <summary>
        /// Hit windows use <paramref name="hitTimingsSecondsFromAttackStart"/> (seconds from attack start).
        /// Use when timings come from animation (e.g. GeisComboData normalized × clip length). Array length = hit count.
        /// </summary>
        public void PerformHitCheck(CombatAction action, float[] hitTimingsSecondsFromAttackStart)
        {
            PerformHitCheck(action, hitTimingsSecondsFromAttackStart, -1);
        }

        /// <summary>
        /// Same as <see cref="PerformHitCheck(CombatAction, float[])"/> but passes Geis weapon slot index (0–3) so
        /// <see cref="IPuzzleMeleeHitSink"/> implementations can filter (e.g. sword vs knife) using the same overlap spheres as combat.
        /// </summary>
        public void PerformHitCheck(CombatAction action, float[] hitTimingsSecondsFromAttackStart, int weaponSlotIndex)
        {
            if (action == null)
                return;

            if (_executor == null)
            {
                Debug.LogWarning(
                    "[SimpleAttackHitDetector] No CombatExecutor on this object — cannot apply hits. Add CombatExecutor or remove SimpleAttackHitDetector.",
                    this);
                return;
            }

            bool hasMainEffects = action.effects != null && action.effects.Length > 0;
            bool hasPerHit = action.perHitEffects != null && action.perHitEffects.Length > 0;
            if (!hasMainEffects && !hasPerHit)
                return;

            _hitSequenceId++;
            int sequenceId = _hitSequenceId;

            if (debugLog)
                Debug.Log($"[SimpleAttackHitDetector] PerformHitCheck called for {action.actionName} (seq {sequenceId}) weaponSlot={weaponSlotIndex}");

            StartCoroutine(HitCheckCoroutine(action, sequenceId, hitTimingsSecondsFromAttackStart, weaponSlotIndex));
        }

        private IEnumerator HitCheckCoroutine(CombatAction action, int sequenceId, float[] timesOverride, int weaponSlotIndex)
        {
            int hitCount;
            float[] times;

            if (timesOverride != null && timesOverride.Length > 0)
            {
                hitCount = timesOverride.Length;
                times = new float[hitCount];
                for (int i = 0; i < hitCount; i++)
                    times[i] = Mathf.Max(0f, timesOverride[i]);
            }
            else
            {
                hitCount = (action.isCombo && action.comboHitCount > 1) ? action.comboHitCount : 1;
                times = ResolveHitTimes(hitCount);
            }

            float elapsed = 0f;
            for (int i = 0; i < hitCount; i++)
            {
                if (sequenceId != _hitSequenceId)
                    yield break;

                float targetTime = times[i];
                float wait = Mathf.Max(0f, targetTime - elapsed);
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);
                elapsed = Mathf.Max(elapsed, targetTime);

                if (sequenceId != _hitSequenceId)
                    yield break;

                var targets = FindTargetsInRange();
                if (debugLog)
                    Debug.Log($"[SimpleAttackHitDetector] Hit {i + 1}/{hitCount} found {targets.Count} target(s)");
                else if (targets.Count == 0)
                    Debug.Log($"[SimpleAttackHitDetector] Hit {i + 1}/{hitCount} found 0 targets for '{action.actionName}'. Layers={DescribeLayerMask(targetLayers)} (mask value {targetLayers.value}). Increase hitRadius / rangeOffset or set Target Layers to include the boss hurtbox layer.", this);

                if (targets.Count > 0)
                {
                    if (hitCount > 1)
                        _executor.ApplyActionToTargets(action, targets, i + 1);
                    else
                        _executor.ApplyActionToTargets(action, targets);
                }

                NotifySwordPuzzleTriggers(action, i + 1, weaponSlotIndex);
            }
        }

        /// <summary>
        /// Uses the same sphere centers/radius as <see cref="FindTargetsInRange"/> so sword-break zones align with melee hits.
        /// Call from weapon-hitbox schedules when <see cref="PerformHitCheck"/> is skipped.
        /// </summary>
        public void NotifyPuzzleMeleeHitSinks(CombatAction action, int hitWindowIndex, int weaponSlotIndex)
        {
            NotifySwordPuzzleTriggers(action, hitWindowIndex, weaponSlotIndex);
        }

        /// <summary>
        /// Uses the same sphere centers/radius as <see cref="FindTargetsInRange"/> so sword-break zones align with melee hits.
        /// </summary>
        private void NotifySwordPuzzleTriggers(CombatAction action, int hitWindowIndex, int weaponSlotIndex)
        {
            if (!notifySwordPuzzleTriggers || weaponSlotIndex < 0)
                return;

            var notified = new HashSet<IPuzzleMeleeHitSink>();

            GetMeleeProbeOrigin(out Vector3 meleeOrigin, out Vector3 meleeFwd);
            Vector3 forwardCenterPuzzle = meleeOrigin + meleeFwd * rangeOffset + Vector3.up * 0.5f;
            Vector3 playerCenterPuzzle = meleeOrigin + Vector3.up * 0.5f;

            void CollectFromSphere(Vector3 center)
            {
                Collider[] cols = Physics.OverlapSphere(center, hitRadius, puzzleProbeLayers, QueryTriggerInteraction.Collide);
                for (int c = 0; c < cols.Length; c++)
                {
                    var col = cols[c];
                    if (col == null) continue;
                    if (col.transform == transform || col.transform.IsChildOf(transform))
                        continue;

                    var sink = col.GetComponentInParent<IPuzzleMeleeHitSink>();
                    if (sink == null || notified.Contains(sink))
                        continue;
                    notified.Add(sink);
                    sink.OnMeleeHitFromSimpleAttack(this, action, weaponSlotIndex, hitWindowIndex);
                }
            }

            CollectFromSphere(forwardCenterPuzzle);
            if (usePlayerCenterFallback)
                CollectFromSphere(playerCenterPuzzle);
        }

        /// <summary>
        /// Absolute times (seconds) from attack start for each hit window, length <paramref name="hitCount"/>.
        /// </summary>
        private float[] ResolveHitTimes(int hitCount)
        {
            if (hitCount <= 1)
                return new[] { hitDelay };

            if (hitTimingsFromAttackStart != null && hitTimingsFromAttackStart.Length >= hitCount)
            {
                var t = new float[hitCount];
                for (int i = 0; i < hitCount; i++)
                    t[i] = Mathf.Max(0f, hitTimingsFromAttackStart[i]);
                return t;
            }

            var fallback = new float[hitCount];
            for (int i = 0; i < hitCount; i++)
                fallback[i] = hitDelay + i * spacingBetweenHits;
            return fallback;
        }

        private void GetMeleeProbeOrigin(out Vector3 origin, out Vector3 planarForward)
        {
            if (OverrideMeleeProbeOrigin != null)
            {
                (origin, planarForward) = OverrideMeleeProbeOrigin();
                return;
            }

            origin = transform.position;
            planarForward = transform.forward;
            planarForward.y = 0f;
            if (planarForward.sqrMagnitude > 1e-6f)
                planarForward.Normalize();
            else
                planarForward = transform.forward;
        }

        private List<CombatEntity> FindTargetsInRange()
        {
            var results = new List<CombatEntity>();
            var seen = new HashSet<CombatEntity>();

            GetMeleeProbeOrigin(out Vector3 probeOrigin, out Vector3 planarForward);
            Vector3 forwardCenter = probeOrigin + planarForward * rangeOffset + Vector3.up * 0.5f;
            Collider[] colliders = Physics.OverlapSphere(
                forwardCenter,
                hitRadius,
                targetLayers,
                combatOverlapTriggerInteraction);

            if (usePlayerCenterFallback)
            {
                Vector3 playerCenter = probeOrigin + Vector3.up * 0.5f;
                Collider[] fallbackColliders = Physics.OverlapSphere(
                    playerCenter,
                    hitRadius,
                    targetLayers,
                    combatOverlapTriggerInteraction);
                var combined = new List<Collider>(colliders);
                foreach (var col in fallbackColliders)
                {
                    if (col != null && !combined.Contains(col))
                        combined.Add(col);
                }
                colliders = combined.ToArray();
            }

            if (debugLog && colliders.Length > 0)
                Debug.Log($"[SimpleAttackHitDetector] OverlapSphere found {colliders.Length} collider(s)");

            foreach (var col in colliders)
            {
                if (col.transform == transform || col.transform.IsChildOf(transform))
                    continue;

                if (_combatEntity != null && col.GetComponent<CombatEntity>() == _combatEntity)
                    continue;

                var entity = col.GetComponent<CombatEntity>() ?? col.GetComponentInParent<CombatEntity>();
                if (entity == null)
                {
                    if (debugLog && (_combatEntity == null || col.gameObject != _combatEntity.gameObject))
                        Debug.Log($"[SimpleAttackHitDetector] Skipped {col.gameObject.name}: no CombatEntity");
                    continue;
                }

                if (entity == _combatEntity)
                    continue;

                if (seen.Contains(entity))
                    continue;

                if (!IsValidTarget(entity))
                {
                    if (debugLog)
                        Debug.Log($"[SimpleAttackHitDetector] Skipped {entity.gameObject.name}: invalid target (tag={entity.gameObject.tag})");
                    continue;
                }

                var gate = entity.GetComponentInParent<IPhysicalWeaponHitGate>();
                if (gate != null && !gate.AllowsPhysicalWeaponHits())
                {
                    if (debugLog)
                        Debug.Log($"[SimpleAttackHitDetector] Skipped {entity.gameObject.name}: physical weapon hits gated off.");
                    continue;
                }

                var data = entity.GetEntityData();
                if (data == null || !data.IsAlive)
                {
                    if (debugLog)
                        Debug.Log($"[SimpleAttackHitDetector] Skipped {entity.gameObject.name}: no data or dead");
                    continue;
                }

                seen.Add(entity);
                results.Add(entity);
            }

            if (debugLog && colliders.Length == 0)
            {
                GetMeleeProbeOrigin(out Vector3 dbgOrigin, out Vector3 _dbgFwd);
                float dist = float.MaxValue;
                var entities = UnityEngine.Object.FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
                foreach (var e in entities)
                {
                    if (e == _combatEntity) continue;
                    float d = Vector3.Distance(dbgOrigin, e.transform.position);
                    if (d < dist) dist = d;
                }

                Vector3 playerCenterDbg = transform.position + Vector3.up * 0.5f;
                int unmaskedFwd = Physics.OverlapSphere(forwardCenter, hitRadius, ~0, combatOverlapTriggerInteraction).Length;
                int unmaskedFeet = usePlayerCenterFallback
                    ? Physics.OverlapSphere(playerCenterDbg, hitRadius, ~0, combatOverlapTriggerInteraction).Length
                    : 0;

                var sb = new StringBuilder();
                sb.Append("[SimpleAttackHitDetector] No colliders with current layer mask. ");
                sb.Append($"Nearest CombatEntity ~{dist:F1}m (straight-line). Sphere fwd center={forwardCenter}, r={hitRadius}. ");
                sb.Append($"TargetLayers: {DescribeLayerMask(targetLayers)} (value {targetLayers.value}). ");
                if (unmaskedFwd + unmaskedFeet > 0)
                    sb.Append($"Same spheres with all layers + triggers: fwd={unmaskedFwd}, feet={unmaskedFeet} → fix Target Layers (often need Default) or move hurtboxes to an included layer. ");
                else if (dist < float.MaxValue && dist > hitRadius * 2f)
                    sb.Append($"Try larger hitRadius (e.g. ≥ {dist:F1}m) or stand closer. ");
                else
                    sb.Append("No physics colliders in the spheres at all — add/enable colliders on hurtboxes or increase radius. ");

                Debug.Log(sb.ToString(), this);
            }

            return results;
        }

        /// <summary>Human-readable layer mask for logs (Unity mask value is not obvious in-editor).</summary>
        private static string DescribeLayerMask(LayerMask mask)
        {
            int v = mask.value;
            if (v == 0)
                return "NONE";
            // Bitmask -1 in Unity is often stored as all bits set for "Everything" in LayerMask UI
            if (v == -1)
                return "Everything";

            var parts = new List<string>(8);
            for (int i = 0; i < 32; i++)
            {
                if ((v & (1 << i)) == 0) continue;
                string name = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(name))
                    name = $"unnamed";
                parts.Add($"{i}:{name}");
            }

            return parts.Count > 0 ? string.Join(", ", parts) : $"raw={v}";
        }

        private bool IsValidTarget(CombatEntity target)
        {
            if (target == _combatEntity)
                return false;

            if (target.simpleMeleeBypassTagFilter)
                return true;

            if (validTargetTags != null && validTargetTags.Length > 0 && !PassesMeleeTargetTagFilter(target.gameObject))
                return false;

            return true;
        }

        private bool PassesMeleeTargetTagFilter(GameObject targetObject)
        {
            for (int i = 0; i < validTargetTags.Length; i++)
            {
                string tag = validTargetTags[i];
                if (string.IsNullOrEmpty(tag))
                    continue;

                if (tag == "Enemy" && IsOnNamedLayer(targetObject, "Enemy"))
                    return true;

                try
                {
                    if (targetObject.CompareTag(tag))
                        return true;

                    Transform root = targetObject.transform.root;
                    if (root != null && root != targetObject.transform && root.CompareTag(tag))
                        return true;
                }
                catch (UnityException)
                {
                    // Undefined tag in Tag Manager — skip.
                }
            }

            return false;
        }

        private static bool IsOnNamedLayer(GameObject gameObject, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 && gameObject != null && gameObject.layer == layer;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 forwardCenter = transform.position + transform.forward * rangeOffset + Vector3.up * 0.5f;
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(forwardCenter, hitRadius);
            if (usePlayerCenterFallback)
            {
                Vector3 playerCenter = transform.position + Vector3.up * 0.5f;
                Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
                Gizmos.DrawSphere(playerCenter, hitRadius);
            }
        }
    }
}
