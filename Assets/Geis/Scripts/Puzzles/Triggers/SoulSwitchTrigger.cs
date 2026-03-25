using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.Puzzles
{
    /// <summary>
    /// A lever or button the player interacts with (E key) while in the soul realm.
    /// Can be configured as a latching toggle or a momentary hold switch.
    ///
    /// Default realm: SoulOnly.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SoulSwitchTrigger : PuzzleTriggerBase
    {
        [Header("Interaction")]
        [Tooltip("Latching toggle (press once = on, press again = off) vs momentary (hold = on).")]
        [SerializeField] private bool isToggle = true;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private Key interactionKey = Key.E;

        [Header("Prompt")]
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.8f, 0f);

        private bool _playerInRange;
        private GameObject _activePrompt;
        private GameObject _cachedPlayer;
        private float _playerSearchTimer;

        private void Update()
        {
            if (!IsAccessibleInCurrentRealm())
            {
                SetInRange(false);
                return;
            }

            RefreshPlayerDistance();

            if (_playerInRange && Keyboard.current != null &&
                Keyboard.current[interactionKey].wasPressedThisFrame)
            {
                if (isToggle)
                    SetActivated(!IsActivated);
                else
                    SetActivated(true);
            }

            if (!isToggle && _playerInRange && Keyboard.current != null &&
                Keyboard.current[interactionKey].wasReleasedThisFrame)
            {
                SetActivated(false);
            }
        }

        private void RefreshPlayerDistance()
        {
            _playerSearchTimer -= Time.deltaTime;
            if (_cachedPlayer == null || _playerSearchTimer <= 0f)
            {
                _cachedPlayer = GameObject.FindGameObjectWithTag("Player");
                _playerSearchTimer = 0.5f;
            }

            if (_cachedPlayer == null)
            {
                SetInRange(false);
                return;
            }

            bool inRange = Vector3.Distance(transform.position, _cachedPlayer.transform.position)
                           <= interactionRange;
            SetInRange(inRange);
        }

        private void SetInRange(bool inRange)
        {
            if (_playerInRange == inRange) return;
            _playerInRange = inRange;

            if (inRange && _activePrompt == null)
                ShowPrompt();
            else if (!inRange && _activePrompt != null)
                HidePrompt();
        }

        private void ShowPrompt()
        {
            if (promptPrefab != null)
            {
                _activePrompt = Instantiate(promptPrefab, transform.position + promptOffset,
                    Quaternion.identity, transform);
            }
        }

        private void HidePrompt()
        {
            if (_activePrompt != null)
            {
                Destroy(_activePrompt);
                _activePrompt = null;
            }
        }

        private void OnDestroy() => HidePrompt();
    }
}
