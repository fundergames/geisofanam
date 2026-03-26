using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Bow puzzle trigger. Activates when an arrow (<see cref="Projectile"/>) arrives
    /// within <see cref="detectionRadius"/> of this target.
    ///
    /// Uses <c>Physics.OverlapSphere</c> instead of <c>OnTriggerEnter</c> because
    /// <see cref="Projectile"/> moves via <c>transform.position</c> on a kinematic
    /// Rigidbody, which can miss physics trigger callbacks on fast-moving objects.
    ///
    /// This is the simple "shoot the distant node" mechanic. For the full
    /// mark-then-shoot mechanic see <see cref="BowMarkTargetTrigger"/>.
    ///
    /// Default realm: PhysicalOnly.
    /// </summary>
    [RequireComponent(typeof(Collider))]
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

        private float _scanTimer;
        private const float ScanInterval = 1f / 30f; // ~30 Hz

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            SetVFX(false);
        }

        private void Update()
        {
            if (IsActivated) return;
            if (!IsAccessibleInCurrentRealm()) return;

            _scanTimer += Time.deltaTime;
            if (_scanTimer < ScanInterval) return;
            _scanTimer = 0f;

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
            foreach (var col in hits)
            {
                if (col.GetComponent<Projectile>() != null)
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
