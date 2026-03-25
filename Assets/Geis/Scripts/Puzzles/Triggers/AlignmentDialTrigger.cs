using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.Puzzles
{
    /// <summary>
    /// Alignment puzzle. Player holds E near a spectral dial and uses horizontal input to
    /// rotate it. When the dial's angle is within snapThreshold degrees of targetAngle it
    /// snaps and activates.
    ///
    /// Default realm: SoulOnly.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AlignmentDialTrigger : PuzzleTriggerBase
    {
        [Header("Dial Settings")]
        [Tooltip("Rotation axis in local space.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [Tooltip("Target angle in degrees around the rotation axis.")]
        [SerializeField] private float targetAngle = 0f;
        [Tooltip("Degrees per second when player is turning the dial.")]
        [SerializeField] private float rotationSpeed = 90f;
        [Tooltip("Activates when within this many degrees of the target.")]
        [SerializeField] private float snapThreshold = 15f;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private Key   holdKey = Key.E;
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private Vector3    promptOffset = new Vector3(0f, 1.8f, 0f);

        [Header("Visual")]
        [Tooltip("The Transform to visually rotate (the dial mesh).")]
        [SerializeField] private Transform dialVisual;

        [Header("Audio")]
        [SerializeField] private AudioClip snapSound;
        [SerializeField] private AudioSource audioSource;

        private float _currentAngle;
        private bool  _playerInRange;
        private bool  _snapped;
        private GameObject _activePrompt;
        private GameObject _cachedPlayer;
        private float _playerSearchTimer;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (!IsAccessibleInCurrentRealm())
            {
                SetInRange(false);
                return;
            }

            RefreshPlayerDistance();

            if (_snapped) return;

            bool holding = _playerInRange && Keyboard.current != null &&
                           Keyboard.current[holdKey].isPressed;

            if (holding)
            {
                // Read horizontal axis from both keyboard and gamepad
                float h = 0f;
                if (Keyboard.current[Key.A].isPressed || Keyboard.current[Key.LeftArrow].isPressed) h = -1f;
                if (Keyboard.current[Key.D].isPressed || Keyboard.current[Key.RightArrow].isPressed) h =  1f;
                if (Gamepad.current != null) h += Gamepad.current.leftStick.x.ReadValue();
                h = Mathf.Clamp(h, -1f, 1f);

                _currentAngle += h * rotationSpeed * Time.deltaTime;
                _currentAngle  = (_currentAngle % 360f + 360f) % 360f;
                ApplyRotation(_currentAngle);

                // Snap check
                float delta = Mathf.Abs(Mathf.DeltaAngle(_currentAngle, targetAngle));
                if (delta <= snapThreshold)
                    Snap();
            }
        }

        private void Snap()
        {
            _snapped      = true;
            _currentAngle = targetAngle;
            ApplyRotation(targetAngle);
            HidePrompt();

            if (snapSound != null && audioSource != null)
                audioSource.PlayOneShot(snapSound);

            SetActivated(true);
        }

        private void ApplyRotation(float angle)
        {
            if (dialVisual == null) return;
            dialVisual.localRotation = Quaternion.AngleAxis(angle, rotationAxis);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            _snapped = false;
        }

        private void RefreshPlayerDistance()
        {
            _playerSearchTimer -= Time.deltaTime;
            if (_cachedPlayer == null || _playerSearchTimer <= 0f)
            {
                _cachedPlayer     = GameObject.FindGameObjectWithTag("Player");
                _playerSearchTimer = 0.5f;
            }

            bool inRange = _cachedPlayer != null &&
                           Vector3.Distance(transform.position, _cachedPlayer.transform.position) <= interactionRange;
            SetInRange(inRange);
        }

        private void SetInRange(bool inRange)
        {
            if (_playerInRange == inRange) return;
            _playerInRange = inRange;
            if (inRange && _activePrompt == null) ShowPrompt();
            else if (!inRange)                    HidePrompt();
        }

        private void ShowPrompt()
        {
            if (promptPrefab != null)
                _activePrompt = Instantiate(promptPrefab, transform.position + promptOffset,
                    Quaternion.identity, transform);
        }

        private void HidePrompt()
        {
            if (_activePrompt != null) { Destroy(_activePrompt); _activePrompt = null; }
        }

        private void OnDestroy() => HidePrompt();
    }
}
