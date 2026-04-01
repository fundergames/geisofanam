using Geis.InteractInput;
using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Combined mark-then-shoot bow puzzle trigger. Directly models the design doc mechanic:
    ///   1. Enter soul realm → walk within range → press X to mark the node (soul-realm phase).
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
    [DefaultExecutionOrder(-50)]
    public class BowMarkTargetTrigger : PuzzleTriggerBase
    {
        /// <summary>Mark phase and shoot phase span both realms.</summary>
        public override PuzzleRealmMode RealmMode => PuzzleRealmMode.BothRealms;

        [Header("Mark (Soul Realm)")]
        [SerializeField] private float markRange = 5f;
        [Tooltip("Max distance to show the mark prompt. If < 0, uses Mark Range.")]
        [SerializeField] private float markPromptRange = -1f;
        [Tooltip("Seconds the mark persists. 0 = permanent until shot or reset.")]
        [SerializeField] private float markDuration = 0f;
        [SerializeField] private GameObject markPromptPrefab;
        [SerializeField] private Vector3    markPromptOffset = new Vector3(0f, 1.8f, 0f);
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
        private GameObject  _markPrompt;

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
                HideMarkPrompt();

            // Expire mark timer
            if (_isMarked && markDuration > 0f)
            {
                _markTimer -= Time.deltaTime;
                if (_markTimer <= 0f)
                    ClearMark();
            }
        }

        private void LateUpdate()
        {
            if (IsActivated) return;
            if (!IsAccessibleInCurrentRealm()) return;

            bool soulActive = Geis.SoulRealm.SoulRealmManager.Instance != null &&
                              Geis.SoulRealm.SoulRealmManager.Instance.IsSoulRealmActive;
            if (!soulActive)
                HandleShootPhase();
        }

        // ── Mark phase (soul realm) ──────────────────────────────────────────────

        private void HandleMarkPhase()
        {
            if (_isMarked)
            {
                HideMarkPrompt();
                return;
            }

            Vector3 playerPos = GeisInteractInput.GetInteractionWorldPositionOrFallback();
            float dist = Vector3.Distance(transform.position, playerPos);
            bool inMarkRange = dist <= markRange;
            float promptCutoff = markPromptRange >= 0f ? markPromptRange : markRange;
            bool inPromptRange = dist <= promptCutoff;

            if (!inMarkRange)
            {
                HideMarkPrompt();
                return;
            }

            if (inPromptRange && _markPrompt == null)
                ShowMarkPrompt();
            else if (!inPromptRange)
                HideMarkPrompt();

            // HandleMarkPhase only runs while soul realm is active; use raw interact (realm matches manager, not input provider).
            if (GeisInteractInput.WasInteractPressedThisFrame())
                ApplyMark();
        }

        private void ShowMarkPrompt()
        {
            if (markPromptPrefab != null)
            {
                _markPrompt = Instantiate(markPromptPrefab, transform.position + markPromptOffset,
                    Quaternion.identity, transform);
            }
            else
            {
                _markPrompt = PuzzleInteractionPrompt.CreateWorldLetterPrompt(transform, markPromptOffset, "X");
            }
        }

        private void HideMarkPrompt()
        {
            if (_markPrompt != null)
            {
                Destroy(_markPrompt);
                _markPrompt = null;
            }
        }

        private void ApplyMark()
        {
            HideMarkPrompt();
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

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, ~0, QueryTriggerInteraction.Collide);
            foreach (var col in hits)
            {
                if (col.GetComponentInParent<Projectile>() != null)
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
            HideMarkPrompt();
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            ClearMark();
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
