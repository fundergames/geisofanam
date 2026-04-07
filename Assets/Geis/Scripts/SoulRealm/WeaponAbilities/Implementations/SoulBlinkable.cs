using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute: pose A/B or a <see cref="SoulBlinkSocket"/> for pose B.
    /// With <see cref="SoulBlinkManipulationController"/>, Q grabs the object (character stands still), it floats with look,
    /// and snaps when near the socket; Q again cancels and returns to pose A.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulBlinkable : MonoBehaviour
    {
        [Tooltip("World or local anchor for pose A (uses this transform if null).")]
        [SerializeField] private Transform poseA;

        [Tooltip("Optional pose B when not using a Soul Blink Socket.")]
        [SerializeField] private Transform poseB;

        [Tooltip("If set, swapping to pose B snaps to this socket's anchor (world pose). Leave pose B empty when using only the socket.")]
        [SerializeField] private SoulBlinkSocket socketB;

        [SerializeField] private bool useLocalPositions = true;
        [SerializeField] private AudioClip swapSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Manipulation VFX")]
        [Tooltip("Optional looping particles/effects parented to this object while Soul Blink is moving it. If null, the controller uses its default or procedural carry loop.")]
        [SerializeField] private GameObject manipulationCarryVfxPrefab;

        private bool _atA = true;

        /// <summary>When <see cref="poseA"/> is this transform, we restore to these snapshots — copying
        /// <c>transform</c> onto itself on the return swap would otherwise do nothing.</summary>
        private Vector3 _poseAInitialLocalPos;
        private Quaternion _poseAInitialLocalRot;
        private Vector3 _poseAInitialWorldPos;
        private Quaternion _poseAInitialWorldRot;
        private bool _poseAIsThisTransform;

        public bool AtPoseA => _atA;

        public SoulBlinkSocket SocketB => socketB;

        public GameObject ManipulationCarryVfxPrefab => manipulationCarryVfxPrefab;

        private void Awake()
        {
            if (poseA == null)
                poseA = transform;
            if (poseB == null && socketB == null)
                Debug.LogWarning(
                    $"[SoulBlinkable] Pose B and Socket B unset on {name} — assign a pose B transform and/or a Soul Blink Socket.",
                    this);
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            _poseAIsThisTransform = poseA == transform;
            if (_poseAIsThisTransform)
            {
                _poseAInitialLocalPos = transform.localPosition;
                _poseAInitialLocalRot = transform.localRotation;
                _poseAInitialWorldPos = transform.position;
                _poseAInitialWorldRot = transform.rotation;
            }
        }

        /// <summary>Instantly moves this object to the other pose (A ↔ B).</summary>
        public void Swap()
        {
            if (poseA == null)
                return;
            if (_atA && poseB == null && socketB == null)
                return;

            if (_atA)
                MoveToPoseB();
            else
                MoveToPoseA();

            _atA = !_atA;

            PlaySwapSound();
        }

        /// <summary>Snaps into the socket (e.g. proximity from <see cref="SoulBlinkManipulationController"/>).</summary>
        public void SnapIntoSocketFromRaycast(SoulBlinkSocket socket)
        {
            if (socket == null)
                return;

            ApplySocketAnchor(socket);
            _atA = false;
            PlaySwapSound();
        }

        /// <summary>Returns the movable to pose A (used when canceling manipulation mid-air).</summary>
        public void RestoreToPoseA()
        {
            if (poseA == null)
                return;
            _atA = true;
            MoveToPoseA();
        }

        private void MoveToPoseB()
        {
            if (socketB != null)
                ApplySocketAnchor(socketB);
            else
                ApplyTransformAsPoseB(poseB);
        }

        private void ApplySocketAnchor(SoulBlinkSocket socket)
        {
            Transform anchor = socket.SnapAnchor != null ? socket.SnapAnchor : socket.transform;
            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        private void ApplyTransformAsPoseB(Transform t)
        {
            if (t == null)
                return;

            if (useLocalPositions)
            {
                transform.localPosition = t.localPosition;
                transform.localRotation = t.localRotation;
            }
            else
            {
                transform.position = t.position;
                transform.rotation = t.rotation;
            }
        }

        private void MoveToPoseA()
        {
            if (useLocalPositions)
            {
                if (poseA == transform && _poseAIsThisTransform)
                {
                    transform.localPosition = _poseAInitialLocalPos;
                    transform.localRotation = _poseAInitialLocalRot;
                }
                else
                {
                    transform.localPosition = poseA.localPosition;
                    transform.localRotation = poseA.localRotation;
                }
            }
            else
            {
                if (poseA == transform && _poseAIsThisTransform)
                {
                    transform.position = _poseAInitialWorldPos;
                    transform.rotation = _poseAInitialWorldRot;
                }
                else
                {
                    transform.position = poseA.position;
                    transform.rotation = poseA.rotation;
                }
            }
        }

        private void PlaySwapSound()
        {
            if (swapSound != null && audioSource != null)
                audioSource.PlayOneShot(swapSound);
        }
    }
}
