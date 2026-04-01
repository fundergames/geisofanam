using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Bow puzzle trigger. Activates when an arrow (<see cref="Projectile"/>) arrives
    /// within <see cref="detectionRadius"/> of this target.
    ///
    /// Uses <c>Physics.OverlapSphere</c> (same probe style as <see cref="RogueDeal.Combat.SimpleAttackHitDetector"/> melee)
    /// instead of relying on <c>OnTriggerEnter</c> alone because
    /// <see cref="Projectile"/> moves via <c>transform.position</c> on a kinematic
    /// Rigidbody, which can miss physics trigger callbacks on fast-moving objects.
    /// Arrows must have a <see cref="Collider"/> on the projectile (see arrow prefab). Detection runs in <c>LateUpdate</c>
    /// after movement and before deferred despawn.
    ///
    /// This is the simple "shoot the distant node" mechanic. For the full
    /// mark-then-shoot mechanic see <see cref="BowMarkTargetTrigger"/>.
    ///
    /// Default realm: PhysicalOnly.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DefaultExecutionOrder(-50)]
    public class BowTargetTrigger : PuzzleTriggerBase
    {
        [Header("Detection")]
        [Tooltip("Radius around this target within which an arrow counts as a hit.")]
        [SerializeField] private float detectionRadius = 1f;

        [Header("Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Visual")]
        [Tooltip("Optional object shown when not yet hit (e.g. a glowing target ring).")]
        [SerializeField] private GameObject idleVFX;
        [Tooltip("Optional object shown after being hit.")]
        [SerializeField] private GameObject hitVFX;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            SetVFX(false);
        }

        /// <summary>
        /// LateUpdate: runs after <see cref="Projectile"/> movement in Update but before its deferred despawn,
        /// so the arrow still exists when we overlap-test.
        /// </summary>
        private void LateUpdate()
        {
            if (IsActivated) return;
            if (!IsAccessibleInCurrentRealm()) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, ~0, QueryTriggerInteraction.Collide);
            foreach (var col in hits)
            {
                if (col.GetComponentInParent<Projectile>() != null)
                {
                    RegisterHit();
                    return;
                }
            }
        }

        private void RegisterHit()
        {
            if (hitSound != null && audioSource != null)
                audioSource.PlayOneShot(hitSound);

            SetVFX(true);
            SetActivated(true);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            SetVFX(false);
        }

        private void SetVFX(bool hit)
        {
            if (idleVFX != null) idleVFX.SetActive(!hit);
            if (hitVFX  != null) hitVFX.SetActive(hit);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
