using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// World socket for <see cref="SoulBlinkManipulationController"/> / <see cref="DaggerObjectBlinkSoulWeaponAbility"/>:
    /// when the movable is within <see cref="snapRadius"/> of <see cref="snapAnchor"/>, it snaps into place.
    /// Assign the same socket on the cube as <see cref="SoulBlinkable.SocketB"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulBlinkSocket : MonoBehaviour
    {
        [Tooltip("World pose for the movable when snapped. If null, uses this transform.")]
        [SerializeField] private Transform snapAnchor;

        [Tooltip("Cube / object that blinks into this socket.")]
        [SerializeField] private SoulBlinkable blinkTarget;

        [Tooltip("How close the movable must be (world space) to snap into the anchor.")]
        [SerializeField] private float snapRadius = 0.65f;

        public Transform SnapAnchor => snapAnchor;

        public SoulBlinkable BlinkTarget => blinkTarget;

        public float SnapRadius => snapRadius;

        public Vector3 SnapWorldPosition =>
            snapAnchor != null ? snapAnchor.position : transform.position;

        /// <summary>True when <paramref name="worldPoint"/> is close enough to snap (e.g. while floating the cube).</summary>
        public bool IsWithinSnapRange(Vector3 worldPoint)
        {
            return Vector3.Distance(worldPoint, SnapWorldPosition) <= snapRadius;
        }

        private void OnDrawGizmosSelected()
        {
            var t = snapAnchor != null ? snapAnchor : transform;
            Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.65f);
            Gizmos.DrawWireSphere(t.position, 0.12f);
            Gizmos.DrawLine(t.position, t.position + t.forward * 0.3f);
            Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.25f);
            Gizmos.DrawWireSphere(SnapWorldPosition, snapRadius);
        }
    }
}
