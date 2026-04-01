using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Pluggable dial rotation behaviour. Add a concrete implementation (e.g. <see cref="GeisDialIncrementalStickMovement"/>)
    /// on the same GameObject or child as <see cref="Triggers.AlignmentDialTrigger"/>.
    /// </summary>
    public abstract class GeisDialMovement : MonoBehaviour
    {
        /// <summary>Clear internal timers when the player releases interact or leaves the dial.</summary>
        public virtual void ResetMovementState() { }

        /// <summary>Angle delta in degrees to apply this frame around the dial&apos;s configured axis (positive = one direction).</summary>
        public abstract float ComputeAngleDeltaThisFrame(float deltaTime);
    }
}
