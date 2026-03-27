using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Marks a moving surface (e.g. kinematic puzzle platform) so grounded characters standing on it
    /// receive its motion via <see cref="GroundRideUtility"/>.
    /// </summary>
    public class MovingGroundCarrier : MonoBehaviour
    {
        [Tooltip("World-space transform used for ride delta; defaults to this object.")]
        [SerializeField] private Transform movingReference;

        public Transform MovingTransform => movingReference != null ? movingReference : transform;
    }
}
