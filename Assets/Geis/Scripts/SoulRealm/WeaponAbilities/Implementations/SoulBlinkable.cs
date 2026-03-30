using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute: two logical poses (A/B). <see cref="DaggerObjectBlinkSoulWeaponAbility"/> swaps between them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulBlinkable : MonoBehaviour
    {
        [Tooltip("World or local anchor for pose A (uses this transform if null).")]
        [SerializeField] private Transform poseA;
        [SerializeField] private Transform poseB;
        [SerializeField] private bool useLocalPositions = true;
        [SerializeField] private AudioClip swapSound;
        [SerializeField] private AudioSource audioSource;

        private bool _atA = true;

        public bool AtPoseA => _atA;

        private void Awake()
        {
            if (poseA == null)
                poseA = transform;
            if (poseB == null)
                Debug.LogWarning($"[SoulBlinkable] Pose B unset on {name}.", this);
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        /// <summary>Instantly moves this object to the other pose.</summary>
        public void Swap()
        {
            if (poseA == null || poseB == null)
                return;

            Transform from = _atA ? poseA : poseB;
            Transform to = _atA ? poseB : poseA;

            if (useLocalPositions)
            {
                transform.localPosition = to.localPosition;
                transform.localRotation = to.localRotation;
            }
            else
            {
                transform.position = to.position;
                transform.rotation = to.rotation;
            }

            _atA = !_atA;

            if (swapSound != null && audioSource != null)
                audioSource.PlayOneShot(swapSound);
        }
    }
}
