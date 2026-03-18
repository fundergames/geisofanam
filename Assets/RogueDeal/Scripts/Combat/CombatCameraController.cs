using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatCameraController : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);
        [SerializeField] private float smoothTime = 0.3f;
        [SerializeField] private bool followEnabled = true;
        [Tooltip("If true, camera orbits around the character; look input rotates only the camera. If false, uses world-space offset with full orbit.")]
        [SerializeField] private bool stayBehindCharacter = true;

        [Header("Look At Settings")]
        [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

        [Header("Orbit Rotation (Free Look)")]
        [Tooltip("Assign the CombatInputReader in the scene (e.g. on the player). If unset, will try to find one.")]
        [SerializeField] private CombatInputReader inputProvider;
        [Tooltip("Vertical rotation clamp in degrees")]
        [SerializeField] private float maxPitch = 80f;

        private ICombatInputProvider _input;
        private Vector3 velocity = Vector3.zero;
        private float _orbitYaw;
        private float _orbitPitch;

        private void Awake()
        {
            ResolveInput();
        }

        private void ResolveInput()
        {
            if (_input != null) return;
            _input = inputProvider;
            if (_input == null)
                _input = FindFirstObjectByType<CombatInputReader>();
        }

        private void LateUpdate()
        {
            if (!followEnabled || target == null)
                return;

            ResolveInput();
            if (_input != null)
            {
                CombatInputState state = _input.GetState();
                _orbitYaw += state.Look.x;
                _orbitPitch += state.Look.y;
                _orbitPitch = Mathf.Clamp(_orbitPitch, -maxPitch, maxPitch);
            }

            Vector3 baseOffset;
            if (stayBehindCharacter)
            {
                // Orbit camera around character (world-space); character does not rotate
                baseOffset = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f) * offset;
            }
            else
            {
                baseOffset = (_orbitYaw != 0f || _orbitPitch != 0f)
                    ? Quaternion.Euler(_orbitPitch, _orbitYaw, 0f) * offset
                    : offset;
            }

            Vector3 targetPosition = target.position + baseOffset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

            Vector3 lookAtPosition = target.position + lookAtOffset;
            transform.LookAt(lookAtPosition);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetFollowEnabled(bool enabled)
        {
            followEnabled = enabled;
        }

        public void SnapToTarget()
        {
            if (target != null)
            {
                Vector3 baseOffset = (stayBehindCharacter || _orbitYaw != 0f || _orbitPitch != 0f)
                    ? Quaternion.Euler(_orbitPitch, _orbitYaw, 0f) * offset
                    : offset;
                transform.position = target.position + baseOffset;
                Vector3 lookAtPosition = target.position + lookAtOffset;
                transform.LookAt(lookAtPosition);
            }
        }

        /// <summary>
        /// Sets whether the camera should stay behind the character relative to their forward direction
        /// </summary>
        public void SetStayBehindCharacter(bool enabled)
        {
            stayBehindCharacter = enabled;
        }
    }
}
