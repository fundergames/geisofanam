using System.Collections;
using System.Collections.Generic;
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
        [Tooltip("Log hit checks and target count (for debugging)")]
        [SerializeField] private bool debugLog = false;
        [Tooltip("Layers to check for enemies")]
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
        /// </summary>
        private void NotifySwordPuzzleTriggers(CombatAction action, int hitWindowIndex, int weaponSlotIndex)
        {
            if (!notifySwordPuzzleTriggers || weaponSlotIndex < 0)
                return;

            var notified = new HashSet<IPuzzleMeleeHitSink>();

            void CollectFromSphere(Vector3 center)
            {
                Collider[] cols = Physics.OverlapSphere(center, hitRadius, puzzleProbeLayers);
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

            Vector3 forwardCenter = transform.position + transform.forward * rangeOffset + Vector3.up * 0.5f;
            CollectFromSphere(forwardCenter);
            if (usePlayerCenterFallback)
            {
                Vector3 playerCenter = transform.position + Vector3.up * 0.5f;
                CollectFromSphere(playerCenter);
            }
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

        private List<CombatEntity> FindTargetsInRange()
        {
            var results = new List<CombatEntity>();
            var seen = new HashSet<CombatEntity>();

            Vector3 forwardCenter = transform.position + transform.forward * rangeOffset + Vector3.up * 0.5f;
            Collider[] colliders = Physics.OverlapSphere(forwardCenter, hitRadius, targetLayers);

            if (usePlayerCenterFallback)
            {
                Vector3 playerCenter = transform.position + Vector3.up * 0.5f;
                Collider[] fallbackColliders = Physics.OverlapSphere(playerCenter, hitRadius, targetLayers);
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
                float dist = float.MaxValue;
                var entities = UnityEngine.Object.FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
                foreach (var e in entities)
                {
                    if (e == _combatEntity) continue;
                    float d = Vector3.Distance(transform.position, e.transform.position);
                    if (d < dist) dist = d;
                }
                Debug.Log($"[SimpleAttackHitDetector] No colliders in sphere. Nearest enemy ~{dist:F1}m away. Sphere: fwd={forwardCenter}, r={hitRadius}, layers={targetLayers.value}");
            }

            return results;
        }

        private bool IsValidTarget(CombatEntity target)
        {
            if (target == _combatEntity)
                return false;

            if (validTargetTags != null && validTargetTags.Length > 0)
            {
                bool found = false;
                foreach (var tag in validTargetTags)
                {
                    if (target.gameObject.CompareTag(tag))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }

            return true;
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
