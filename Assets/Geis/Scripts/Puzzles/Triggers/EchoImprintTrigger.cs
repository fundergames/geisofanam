using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.Puzzles
{
    /// <summary>
    /// Echo imprint puzzle. While in the soul realm and within range, the player can press
    /// the echo key (F) to leave a translucent "soul echo" at the ghost's current position.
    /// The echo persists for <see cref="echoDuration"/> seconds and registers as a collider
    /// overlap on a target <see cref="PressurePlateTrigger"/>, keeping it active while the
    /// player is back in the physical world.
    ///
    /// This allows cross-realm timing: the player leaves an echo holding a plate, exits the
    /// soul realm, then does something in the physical world before the echo fades.
    /// </summary>
    public class EchoImprintTrigger : PuzzleTriggerBase
    {
        [Header("Echo Settings")]
        [Tooltip("How long the echo persists in seconds.")]
        [SerializeField] private float echoDuration = 8f;
        [Tooltip("Prefab spawned to represent the echo (should have its own collider tagged for the target plate).")]
        [SerializeField] private GameObject echoPrefab;
        [Tooltip("Tag applied to the echo prefab so a PressurePlateTrigger can detect it.")]
        [SerializeField] private string echoTag = "SoulEcho";
        [SerializeField] private Key    echoKey  = Key.F;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 4f;
        [SerializeField] private GameObject promptPrefab;
        [SerializeField] private Vector3    promptOffset = new Vector3(0f, 2.2f, 0f);

        [Header("Audio")]
        [SerializeField] private AudioClip placeSound;
        [SerializeField] private AudioSource audioSource;

        private GameObject _activeEcho;
        private GameObject _activePrompt;
        private GameObject _cachedGhost;
        private float      _ghostSearchTimer;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (!IsAccessibleInCurrentRealm())
            {
                HidePrompt();
                return;
            }

            RefreshGhostDistance();

            bool inRange = _cachedGhost != null &&
                           Vector3.Distance(transform.position, _cachedGhost.transform.position)
                           <= interactionRange;

            if (inRange && _activePrompt == null)
                ShowPrompt();
            else if (!inRange)
                HidePrompt();

            if (inRange && Keyboard.current != null &&
                Keyboard.current[echoKey].wasPressedThisFrame &&
                _activeEcho == null)
            {
                PlaceEcho();
            }
        }

        private void PlaceEcho()
        {
            if (echoPrefab == null || _cachedGhost == null) return;

            _activeEcho = Instantiate(echoPrefab, _cachedGhost.transform.position,
                _cachedGhost.transform.rotation);
            _activeEcho.tag = echoTag;

            if (placeSound != null && audioSource != null)
                audioSource.PlayOneShot(placeSound);

            SetActivated(true);
            StartCoroutine(EchoLifetime());
        }

        private IEnumerator EchoLifetime()
        {
            yield return new WaitForSeconds(echoDuration);
            FadeEcho();
        }

        private void FadeEcho()
        {
            if (_activeEcho != null)
            {
                Destroy(_activeEcho);
                _activeEcho = null;
            }
            SetActivated(false);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            StopAllCoroutines();
            FadeEcho();
        }

        private void RefreshGhostDistance()
        {
            _ghostSearchTimer -= Time.deltaTime;
            if (_cachedGhost == null || _ghostSearchTimer <= 0f)
            {
                // SoulGhostMotor lives on "SoulGhost" — find by name or a dedicated tag
                var ghostGo = GameObject.Find("SoulGhost");
                _cachedGhost      = ghostGo;
                _ghostSearchTimer = 0.5f;
            }
        }

        private void ShowPrompt()
        {
            if (promptPrefab != null && _activePrompt == null)
                _activePrompt = Instantiate(promptPrefab, transform.position + promptOffset,
                    Quaternion.identity, transform);
        }

        private void HidePrompt()
        {
            if (_activePrompt != null) { Destroy(_activePrompt); _activePrompt = null; }
        }

        private void OnDestroy()
        {
            HidePrompt();
            if (_activeEcho != null) Destroy(_activeEcho);
        }
    }
}
