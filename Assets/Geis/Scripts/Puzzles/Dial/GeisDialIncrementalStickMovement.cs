using Geis.InteractInput;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Steps the dial by a fixed angle when the left stick is held past a deadzone, with a pause between steps.
    /// Positive stick X rotates one way, negative the other (flip <see cref="invertStepSign"/> if needed).
    /// </summary>
    public sealed class GeisDialIncrementalStickMovement : GeisDialMovement
    {
        [SerializeField] private float degreesPerStep = 15f;
        [SerializeField] private float pauseBetweenSteps = 0.25f;
        [SerializeField] private float stickDeadzone = 0.25f;
        [Tooltip("When no gamepad is present, use A/D and arrow keys as full -1/+1 step input.")]
        [SerializeField] private bool allowKeyboardStepWhenNoGamepad = true;

        [Tooltip("Multiply step direction by -1 if the mesh rotates the wrong way.")]
        [SerializeField] private bool invertStepSign;

        private float _cooldown;

        public override void ResetMovementState() => _cooldown = 0f;

        public override float ComputeAngleDeltaThisFrame(float deltaTime)
        {
            float stick = GeisInteractInput.GetGamepadLeftStickHorizontal();
            if (allowKeyboardStepWhenNoGamepad && Mathf.Abs(stick) < 0.01f)
                stick = GeisInteractInput.GetKeyboardHorizontalDigital();

            if (Mathf.Abs(stick) < stickDeadzone)
                return 0f;

            if (_cooldown > 0f)
            {
                _cooldown -= deltaTime;
                return 0f;
            }

            _cooldown = Mathf.Max(0f, pauseBetweenSteps);
            float sign = invertStepSign ? -Mathf.Sign(stick) : Mathf.Sign(stick);
            return sign * Mathf.Abs(degreesPerStep);
        }
    }
}
