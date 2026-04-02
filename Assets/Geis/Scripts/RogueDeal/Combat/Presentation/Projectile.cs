using UnityEngine;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;

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
        private float lifetime = 0f;
        private bool hasArrived = false;
        private Rigidbody rb;
        private GameObject _aimMarker;
        private bool _deferredDespawn;
        private bool _soulMarkSteeringHoming;
        private Vector3 _homingMoveDirection = Vector3.forward;
        private float _homingDistanceReference = 1f;

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
        public void Initialize(Transform target, float speed, BaseEffect[] effects, CombatEntityData attackerData)
        {
            _soulMarkSteeringHoming = false;
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
        public void InitializeSoulMarkHoming(
            Transform target,
            Vector3 initialWorldDirection,
            float speed,
            BaseEffect[] effects,
            CombatEntityData attackerData)
        {
            _soulMarkSteeringHoming = true;
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
        public void InitializeAimPoint(Vector3 aimWorldPoint, float speed, BaseEffect[] effects, CombatEntityData attackerData)
        {
            _soulMarkSteeringHoming = false;
            _aimMarker = new GameObject("_ArrowAimMarker");
            _aimMarker.transform.position = aimWorldPoint;
            Initialize(_aimMarker.transform, speed, effects, attackerData);
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
            if (target != null && effects != null && attackerData != null)
            {
                var targetEntity = target.GetComponent<CombatEntity>();
                if (targetEntity != null)
                {
                    var targetData = targetEntity.GetEntityData();
                    if (targetData != null && targetData.IsAlive)
                    {
                        foreach (var effect in effects)
                        {
                            if (effect == null) continue;
                            
                            var calculated = effect.Calculate(attackerData, targetData, attackerData.equippedWeapon);
                            effect.Apply(targetData, calculated);
                        }
                    }
                }
            }
            
            // Spawn impact effect (optional)
            // TODO: Add impact VFX

            // Defer destroy to LateUpdate so bow puzzle triggers can OverlapSphere the same frame.
            _deferredDespawn = true;
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

