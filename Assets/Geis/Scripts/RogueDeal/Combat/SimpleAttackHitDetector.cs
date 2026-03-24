using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Simple hit detection using OverlapSphere at a delay after attack.
    /// No animation events or weapon colliders needed.
    /// Add to the player and set useWeaponColliders=false on the combat controller.
    /// </summary>
    [RequireComponent(typeof(CombatExecutor))]
    [RequireComponent(typeof(CombatEntity))]
    public class SimpleAttackHitDetector : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Seconds after attack starts before the hit check (tune to match swing animation)")]
        [SerializeField] private float hitDelay = 0.25f;

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

        private CombatExecutor _executor;
        private CombatEntity _combatEntity;

        private void Awake()
        {
            _executor = GetComponent<CombatExecutor>();
            _combatEntity = GetComponent<CombatEntity>();
        }

        /// <summary>
        /// Call this when an attack starts. Performs hit check after hitDelay.
        /// </summary>
        public void PerformHitCheck(CombatAction action)
        {
            if (action == null || action.effects == null || action.effects.Length == 0)
                return;

            if (debugLog)
                Debug.Log($"[SimpleAttackHitDetector] PerformHitCheck called for {action.actionName}");

            StartCoroutine(HitCheckCoroutine(action));
        }

        private IEnumerator HitCheckCoroutine(CombatAction action)
        {
            yield return new WaitForSeconds(hitDelay);

            var targets = FindTargetsInRange();
            if (debugLog)
                Debug.Log($"[SimpleAttackHitDetector] Hit check found {targets.Count} target(s)");

            if (targets.Count > 0)
            {
                _executor.ApplyActionToTargets(action, targets);
            }
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
                if (col.GetComponent<CombatEntity>() == _combatEntity)
                    continue;

                var entity = col.GetComponent<CombatEntity>() ?? col.GetComponentInParent<CombatEntity>();
                if (entity == null)
                {
                    if (debugLog && col.gameObject != _combatEntity.gameObject)
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
