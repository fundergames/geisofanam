using UnityEngine;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using Geis.SoulRealm;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Projectile that moves toward a target and applies effects on arrival.
    /// Uses predicted collision (no physics) to avoid tunneling issues.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(50)]
    public class Projectile : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("If true, logs what this projectile hits and how damage is applied (Editor/Dev builds only).")]
        [SerializeField] private bool debugHits;

        [Header("Projectile Settings")]
        [Tooltip("Speed of the projectile")]
        public float speed = 10f;
        
        [Tooltip("Distance threshold to consider target reached")]
        public float arrivalThreshold = 0.5f;
        
        [Tooltip("Maximum lifetime in seconds")]
        public float maxLifetime = 10f;

        [Header("Soul mark homing (bow)")]
        [Tooltip("Turn rate when the arrow is near the mark (degrees/sec). Blended up from the far rate by distance.")]
        [SerializeField] private float soulMarkHomingTurnRateDegreesPerSecond = 520f;
        [Tooltip("Turn rate when far from the mark — light steering that ramps up as the arrow closes in.")]
        [SerializeField] private float soulMarkHomingTurnRateFarDegreesPerSecond = 96f;
        [Tooltip(">1 keeps steering gentler for more of the flight; 1 = linear blend from far to close.")]
        [SerializeField] private float soulMarkHomingSteerBlendExponent = 1.35f;
        
        private Transform target;
        private BaseEffect[] effects;
        private CombatEntityData attackerData;
        /// <summary>Optional. For aim-point shots the move target is a synthetic marker — damage applies to this entity (e.g. raycast hit).</summary>
        private CombatEntity _aimRayHitEntity;
        /// <summary>Attacker for <see cref="CombatEvents.TriggerDamageApplied"/>.</summary>
        private CombatEntity _sourceEntity;
        private float _damageMultiplier = 1f;
        private float lifetime = 0f;
        private bool hasArrived = false;
        private Rigidbody rb;
        private GameObject _aimMarker;
        private bool _deferredDespawn;
        private bool _soulMarkSteeringHoming;
        private Vector3 _homingMoveDirection = Vector3.forward;
        private float _homingDistanceReference = 1f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void DebugHitLog(string message, CombatEntity targetEntity)
        {
            if (!debugHits) return;
            string targetName = targetEntity != null ? targetEntity.name : "<none>";
            Debug.Log($"[Projectile] {message} (projectile={name}, target={targetName})", this);
        }
#endif

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true; // We'll move manually
            }
        }
        
        /// <summary>
        /// Initializes the projectile with target, speed, effects, and attacker data
        /// </summary>
        public void Initialize(
            Transform target,
            float speed,
            BaseEffect[] effects,
            CombatEntityData attackerData,
            CombatEntity sourceEntity = null,
            float damageMultiplier = 1f)
        {
            _soulMarkSteeringHoming = false;
            _aimRayHitEntity = null;
            _sourceEntity = sourceEntity;
            _damageMultiplier = Mathf.Max(0f, damageMultiplier);
            this.target = target;
            this.speed = speed;
            this.effects = effects;
            this.attackerData = attackerData;
            this.lifetime = 0f;
            this.hasArrived = false;
            
            // Face target
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
        }

        /// <summary>
        /// Bow soul-mark shot: starts along <paramref name="initialWorldDirection"/> then steers toward <paramref name="target"/>.
        /// </summary>
        /// <param name="damageEntityFromAimRay">
        /// Optional. Crosshair raycast hit at fire time (e.g. boss fist). Homing <paramref name="target"/> may be a mark
        /// transform without a <see cref="CombatEntity"/> in its hierarchy; damage still applies to this entity.
        /// </param>
        public void InitializeSoulMarkHoming(
            Transform target,
            Vector3 initialWorldDirection,
            float speed,
            BaseEffect[] effects,
            CombatEntityData attackerData,
            CombatEntity sourceEntity = null,
            CombatEntity damageEntityFromAimRay = null,
            float damageMultiplier = 1f)
        {
            _soulMarkSteeringHoming = true;
            _aimRayHitEntity = damageEntityFromAimRay;
            _sourceEntity = sourceEntity;
            _damageMultiplier = Mathf.Max(0f, damageMultiplier);
            this.target = target;
            this.speed = speed;
            this.effects = effects;
            this.attackerData = attackerData;
            this.lifetime = 0f;
            this.hasArrived = false;

            _homingMoveDirection = initialWorldDirection.sqrMagnitude > 1e-6f
                ? initialWorldDirection.normalized
                : Vector3.forward;
            transform.rotation = Quaternion.LookRotation(_homingMoveDirection);

            Vector3 toMark = target.position - transform.position;
            _homingDistanceReference = Mathf.Max(toMark.magnitude, arrivalThreshold + 0.05f);
        }
        
        /// <summary>
        /// Fires the arrow toward a fixed world-space aim point (camera-forward raycast hit).
        /// The arrow travels in a straight line to that point and despawns on arrival.
        /// </summary>
        /// <param name="entityHitByAimRay"><see cref="CombatEntity"/> from the camera aim raycast (parent lookup). Required for damage — the move target is an empty marker, not the enemy.</param>
        public void InitializeAimPoint(
            Vector3 aimWorldPoint,
            float speed,
            BaseEffect[] effects,
            CombatEntityData attackerData,
            CombatEntity entityHitByAimRay = null,
            CombatEntity sourceEntity = null,
            float damageMultiplier = 1f)
        {
            _soulMarkSteeringHoming = false;
            _aimMarker = new GameObject("_ArrowAimMarker");
            _aimMarker.transform.position = aimWorldPoint;
            _aimRayHitEntity = entityHitByAimRay;
            _sourceEntity = sourceEntity;
            _damageMultiplier = Mathf.Max(0f, damageMultiplier);
            this.target = _aimMarker.transform;
            this.speed = speed;
            this.effects = effects;
            this.attackerData = attackerData;
            this.lifetime = 0f;
            this.hasArrived = false;

            Vector3 direction = (aimWorldPoint - transform.position).normalized;
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        private void Update()
        {
            if (hasArrived) return;
            
            lifetime += Time.deltaTime;
            
            // Check lifetime
            if (lifetime >= maxLifetime)
            {
                Despawn();
                return;
            }
            
            // Check if target is still valid
            if (target == null)
            {
                Despawn();
                return;
            }
            
            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;

            Vector3 direction;
            if (_soulMarkSteeringHoming)
            {
                Vector3 desired = distance > 1e-6f ? toTarget / distance : _homingMoveDirection;
                float farToClose = 1f - Mathf.Clamp01(distance / _homingDistanceReference);
                float steerBlend = Mathf.Pow(Mathf.Clamp01(farToClose), Mathf.Max(0.01f, soulMarkHomingSteerBlendExponent));
                float turnRateDeg = Mathf.Lerp(
                    soulMarkHomingTurnRateFarDegreesPerSecond,
                    soulMarkHomingTurnRateDegreesPerSecond,
                    steerBlend);
                float maxRad = turnRateDeg * Mathf.Deg2Rad * Time.deltaTime;
                _homingMoveDirection = Vector3.RotateTowards(_homingMoveDirection, desired, maxRad, 0f);
                if (_homingMoveDirection.sqrMagnitude < 1e-6f)
                    _homingMoveDirection = desired;
                direction = _homingMoveDirection;
                transform.rotation = Quaternion.LookRotation(direction);
            }
            else
            {
                direction = distance > 1e-6f ? toTarget / distance : Vector3.forward;
                if (direction.sqrMagnitude > 1e-6f)
                    transform.rotation = Quaternion.LookRotation(direction);
            }

            // Check if we've arrived
            if (distance <= arrivalThreshold)
            {
                OnArrival();
                return;
            }

            // Move
            float moveDistance = speed * Time.deltaTime;
            if (moveDistance > distance)
            {
                transform.position = target.position;
                OnArrival();
            }
            else
            {
                transform.position += direction * moveDistance;
            }
        }
        
        private void OnArrival()
        {
            if (hasArrived) return;
            hasArrived = true;
            
            // Apply effects to target
            if (effects != null && attackerData != null)
            {
                CombatEntity targetEntity = _aimRayHitEntity;
                if (targetEntity == null && target != null)
                    targetEntity = target.GetComponent<CombatEntity>() ?? target.GetComponentInParent<CombatEntity>();

                if (targetEntity != null)
                {
                    // Safety: while in Soul Realm, never allow projectiles to apply effects to the shooter's physical self.
                    // (The physical body remains visible and can be ray-hit / targeted.)
                    if (SoulRealmManager.Instance != null
                        && SoulRealmManager.Instance.IsSoulRealmActive
                        && _sourceEntity != null
                        && targetEntity == _sourceEntity)
                    {
                        _deferredDespawn = true;
                        return;
                    }

                    var targetData = targetEntity.GetEntityData();
                    if (targetData != null && targetData.IsAlive)
                    {
                        if (TryApplySoulRealmShieldFromProjectile(targetEntity, targetData))
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            DebugHitLog("Applied damage via ISoulRealmShieldProjectileSink", targetEntity);
#endif
                            _deferredDespawn = true;
                            return;
                        }

                        var physicalGate = targetEntity.GetComponentInParent<IPhysicalWeaponHitGate>();
                        if (physicalGate != null && !physicalGate.AllowsPhysicalWeaponHits())
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            DebugHitLog($"Blocked by IPhysicalWeaponHitGate ({physicalGate.GetType().Name})", targetEntity);
#endif
                            CombatEvents.TriggerDamageApplied(new CombatEventData
                            {
                                source = _sourceEntity,
                                target = targetEntity,
                                damageAmount = 0f,
                                wasCritical = false,
                                wasImmune = true,
                                hitPosition = targetEntity.GetHitPoint()
                            });
                        }
                        else
                        {
                        float hpBefore = targetData.currentHealth;
                        bool wasCritical = false;
                        foreach (var effect in effects)
                        {
                            if (effect == null) continue;

                            var calculated = effect.Calculate(attackerData, targetData, attackerData.equippedWeapon);
                            if (calculated != null && calculated.effectType == EffectType.Damage && _damageMultiplier != 1f)
                                calculated.damageAmount *= _damageMultiplier;
                            if (calculated.wasCritical)
                                wasCritical = true;
                            effect.Apply(targetData, calculated);
                        }

                        float damageDealt = hpBefore - targetData.currentHealth;
                        if (damageDealt > 0f)
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            DebugHitLog($"Dealt damage={damageDealt:F2} (crit={wasCritical})", targetEntity);
#endif
                            CombatEvents.TriggerDamageApplied(new CombatEventData
                            {
                                source = _sourceEntity,
                                target = targetEntity,
                                damageAmount = damageDealt,
                                wasCritical = wasCritical,
                                wasImmune = false,
                                hitPosition = targetEntity.GetHitPoint()
                            });
                        }
                        else
                        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            DebugHitLog("No damage dealt (effects resulted in 0)", targetEntity);
#endif
                        }
                        }
                    }
                }
            }
            
            // Spawn impact effect (optional)
            // TODO: Add impact VFX

            // Defer destroy to LateUpdate so bow puzzle triggers can OverlapSphere the same frame.
            _deferredDespawn = true;
        }

        /// <summary>
        /// Boss fist shields only allow melee while grounded; soul-realm bow shots must drain shield HP instead of tripping the physical gate.
        /// </summary>
        private bool TryApplySoulRealmShieldFromProjectile(CombatEntity targetEntity, CombatEntityData targetData)
        {
            var sink = targetEntity.GetComponentInChildren<ISoulRealmShieldProjectileSink>(true);
            if (sink == null)
                return false;

            PreviewEffectsOutcome(effects, attackerData, targetData, out float previewDamage, out bool wasCritical);
            float shieldDamage = previewDamage;
            if (!sink.TryConsumeSoulRealmProjectileDamage(ref shieldDamage, targetEntity.GetHitPoint()))
                return false;

            CombatEvents.TriggerDamageApplied(new CombatEventData
            {
                source = _sourceEntity,
                target = targetEntity,
                damageAmount = shieldDamage,
                wasCritical = wasCritical,
                wasImmune = false,
                hitPosition = targetEntity.GetHitPoint()
            });
            return true;
        }

        private void PreviewEffectsOutcome(
            BaseEffect[] effectsList,
            CombatEntityData attacker,
            CombatEntityData target,
            out float damageSum,
            out bool anyCrit)
        {
            damageSum = 0f;
            anyCrit = false;
            if (effectsList == null || attacker == null || target == null)
                return;

            Weapon weapon = attacker.equippedWeapon;
            foreach (var effect in effectsList)
            {
                if (effect == null) continue;

                var calculated = effect.Calculate(attacker, target, weapon);
                if (calculated != null && calculated.effectType == EffectType.Damage && _damageMultiplier != 1f)
                    calculated.damageAmount *= _damageMultiplier;
                if (calculated.wasCritical)
                    anyCrit = true;
                if (calculated.damageAmount > 0f)
                    damageSum += calculated.damageAmount;
            }
        }

        private void LateUpdate()
        {
            if (!_deferredDespawn) return;
            _deferredDespawn = false;
            Despawn();
        }

        private void Despawn()
        {
            if (_aimMarker != null)
            {
                Destroy(_aimMarker);
                _aimMarker = null;
            }
            Destroy(gameObject);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Optional: Handle collision with environment
            // For now, we use predicted collision only
        }
    }
}

