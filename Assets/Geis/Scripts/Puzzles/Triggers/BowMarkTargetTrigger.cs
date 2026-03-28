using RogueDeal.Combat.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.Puzzles
{
    /// <summary>
    /// Combined mark-then-shoot bow puzzle trigger. Directly models the design doc mechanic:
    ///   1. Enter soul realm → walk within range → press E to mark the node (soul-realm phase).
    ///   2. Exit soul realm → shoot an arrow at the node (physical-realm phase).
    ///   3. Arrow arrives while node is marked → trigger activates.
    ///
    /// If <see cref="markDuration"/> > 0 the mark expires after that many seconds,
    /// forcing the player to act quickly after exiting the soul realm.
    /// If 0 the mark persists indefinitely.
    ///
    /// Realm mode: BothRealms (the two phases operate in different realms internally).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BowMarkTargetTrigger : PuzzleTriggerBase
    {
        /// <summary>Mark phase and shoot phase span both realms.</summary>
        public override PuzzleRealmMode RealmMode => PuzzleRealmMode.BothRealms;

        [Header("Mark (Soul Realm)")]
        [SerializeField] private float markRange = 5f;
        [Tooltip("Seconds the mark persists. 0 = permanent until shot or reset.")]
        [SerializeField] private float markDuration = 0f;
        [SerializeField] private Key   markKey = Key.E;
        [SerializeField] private GameObject markVFXPrefab;
        [SerializeField] private Vector3    markVFXOffset = new Vector3(0f, 0.5f, 0f);
        [SerializeField] private AudioClip  markSound;

        [Header("Shoot (Physical Realm)")]
        [Tooltip("Radius around this node within which an arrow counts as a hit.")]
        [SerializeField] private float detectionRadius = 1f;
        [SerializeField] private AudioClip shootSound;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private bool        _isMarked;
        private float       _markTimer;
        private GameObject  _markVFX;
        private GameObject  _cachedPlayer;
        private float       _playerSearchTimer;
        private float       _scanTimer;
        private const float ScanInterval = 1f / 30f;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (IsActivated) return;
            if (!IsAccessibleInCurrentRealm()) return;

            bool soulActive = Geis.SoulRealm.SoulRealmManager.Instance != null &&
                              Geis.SoulRealm.SoulRealmManager.Instance.IsSoulRealmActive;

            if (soulActive)
                HandleMarkPhase();
            else
                HandleShootPhase();

            // Expire mark timer
            if (_isMarked && markDuration > 0f)
            {
                _markTimer -= Time.deltaTime;
                if (_markTimer <= 0f)
                    ClearMark();
            }
        }

        // ── Mark phase (soul realm) ──────────────────────────────────────────────

        private void HandleMarkPhase()
        {
            if (_isMarked) return;

            RefreshPlayerRef();
            if (_cachedPlayer == null) return;

            bool inRange = Vector3.Distance(transform.position, _cachedPlayer.transform.position) <= markRange;
            if (!inRange) return;

            if (Keyboard.current != null && Keyboard.current[markKey].wasPressedThisFrame)
                ApplyMark();
        }

        private void ApplyMark()
        {
            _isMarked  = true;
            _markTimer = markDuration > 0f ? markDuration : float.MaxValue;

            if (markVFXPrefab != null)
                _markVFX = Instantiate(markVFXPrefab, transform.position + markVFXOffset,
                    Quaternion.identity, transform);

            if (markSound != null && audioSource != null)
                audioSource.PlayOneShot(markSound);
        }

        // ── Shoot phase (physical realm) ─────────────────────────────────────────

        private void HandleShootPhase()
        {
            if (!_isMarked) return;

            _scanTimer += Time.deltaTime;
            if (_scanTimer < ScanInterval) return;
            _scanTimer = 0f;

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
            foreach (var col in hits)
            {
                if (col.GetComponent<Projectile>() != null)
                {
                    RegisterShot();
                    return;
                }
            }
        }

        private void RegisterShot()
        {
            if (shootSound != null && audioSource != null)
                audioSource.PlayOneShot(shootSound);

            ClearMark();
            SetActivated(true);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void ClearMark()
        {
            _isMarked = false;
            if (_markVFX != null) { Destroy(_markVFX); _markVFX = null; }
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            ClearMark();
        }

        private void RefreshPlayerRef()
        {
            _playerSearchTimer -= Time.deltaTime;
            if (_cachedPlayer == null || _playerSearchTimer <= 0f)
            {
                _cachedPlayer      = GameObject.FindGameObjectWithTag("Player");
                _playerSearchTimer = 0.5f;
            }
        }

        private void OnDestroy() => ClearMark();

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, markRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
