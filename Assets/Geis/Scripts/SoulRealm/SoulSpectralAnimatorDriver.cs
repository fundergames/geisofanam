using Geis.Animation;
using Geis.InputSystem;
using Geis.Locomotion;
using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Drives the spectral copy's Animator with the same locomotion parameters as
    /// <see cref="GeisPlayerAnimationController"/> via <see cref="LocomotionAnimatorApplier"/>.
    /// </summary>
    public sealed class SoulSpectralAnimatorDriver : MonoBehaviour
    {
        private const float AnimationDampTime = 5f;
        private const float StrafeDirectionDampTime = 20f;

        private enum GaitState
        {
            Idle,
            Walk,
            Run,
            Sprint
        }

        [SerializeField] private Animator animator;
        private SoulGhostMotor motor;
        private GeisInputReader inputReader;
        private GeisPlayerAnimationController bodyLocomotion;
        private bool _hasFallingBlendParameter;

        private float movementInputDuration;
        private bool movementInputHeld;
        private bool movementInputPressed;
        private bool movementInputTapped;

        private float shuffleDirectionX;
        private float shuffleDirectionZ;
        private float strafeDirectionX;
        private float strafeDirectionZ;
        private float forwardStrafe = 1f;
        private float cameraRotationOffset;
        private float strafeAngle;
        private GaitState currentGait;
        private float fallingDuration;
        private Vector3 cameraForward;
        private Vector3 moveDirection;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void Configure(SoulGhostMotor ghostMotor, GeisInputReader reader, GeisPlayerAnimationController body)
        {
            motor = ghostMotor;
            inputReader = reader;
            bodyLocomotion = body;
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            _hasFallingBlendParameter = animator != null && AnimatorParameterGuard.HasParameter(animator, "FallingBlend");
        }

        private void LateUpdate()
        {
            if (animator == null || motor == null || bodyLocomotion == null || inputReader == null ||
                !animator.isActiveAndEnabled)
                return;
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                return;

            var cam = bodyLocomotion.CameraControllerRef;
            if (cam == null)
                return;

            UpdateMovementInputFlags();
            moveDirection = cam.GetCameraForwardZeroedYNormalised() * inputReader._moveComposite.y
                + cam.GetCameraRightZeroedYNormalised() * inputReader._moveComposite.x;

            bool grounded = motor.IsGroundedPublic;
            if (!grounded)
                fallingDuration += Time.deltaTime;
            else
                fallingDuration = 0f;

            float speed2D = motor.MirrorSpeed2D;

            bool isStrafingAnim = bodyLocomotion.LocomotionAnimatorUsesStrafeStyle;
            FaceMoveDirection(cam, isStrafingAnim);
            CalculateGait(speed2D);
            bool isStopped = moveDirection.magnitude < 0.01f && speed2D < 0.5f;

            bool airGait = bodyLocomotion.LocomotionAirGaitForAnimator;

            var snap = new LocomotionPresentationSnapshot
            {
                LeanValue = 0f,
                HeadLookX = 0f,
                HeadLookY = 0f,
                BodyLookX = 0f,
                BodyLookY = 0f,
                IsStrafingFloat = isStrafingAnim ? 1f : 0f,
                InclineAngle = 0f,
                MoveSpeed2D = speed2D,
                CurrentGait = (int)currentGait,
                StrafeDirectionX = strafeDirectionX,
                StrafeDirectionZ = strafeDirectionZ,
                ForwardStrafe = forwardStrafe,
                CameraRotationOffset = cameraRotationOffset,
                MovementInputHeld = movementInputHeld,
                MovementInputPressed = movementInputPressed,
                MovementInputTapped = movementInputTapped,
                ShuffleDirectionX = shuffleDirectionX,
                ShuffleDirectionZ = shuffleDirectionZ,
                IsTurningInPlace = false,
                IsCrouching = bodyLocomotion.LocomotionIsCrouching,
                FallingDuration = fallingDuration,
                IsGrounded = grounded,
                IsWalking = bodyLocomotion.LocomotionIsWalking,
                IsStopped = isStopped,
                IsStarting = false,
                LocomotionStartDirection = 0f
            };

            var ctx = new LocomotionApplyContext
            {
                AirGaitForAnimator = airGait,
                HasFallingBlendParameter = _hasFallingBlendParameter && bodyLocomotion != null,
                FallingBlendValue = bodyLocomotion != null
                    ? bodyLocomotion.GetFallingBlendParameter(fallingDuration)
                    : 0f,
                SetIsJumping = true,
                IsJumpingValue = !grounded && motor.VerticalVelocity > 0.5f
            };

            LocomotionAnimatorApplier.ApplySyntyLocomotion(animator, snap, ctx);
        }

        private void UpdateMovementInputFlags()
        {
            float hold = bodyLocomotion.LocomotionButtonHoldThreshold;
            if (inputReader._movementInputDetected)
            {
                if (movementInputDuration == 0f)
                    movementInputTapped = true;
                else if (movementInputDuration > 0f && movementInputDuration < hold)
                {
                    movementInputTapped = false;
                    movementInputPressed = true;
                    movementInputHeld = false;
                }
                else
                {
                    movementInputTapped = false;
                    movementInputPressed = false;
                    movementInputHeld = true;
                }

                movementInputDuration += Time.deltaTime;
            }
            else
            {
                movementInputDuration = 0f;
                movementInputTapped = false;
                movementInputPressed = false;
                movementInputHeld = false;
            }
        }

        private void FaceMoveDirection(GeisCameraController cam, bool isStrafing)
        {
            Transform t = motor.transform;
            Vector3 characterForward = new Vector3(t.forward.x, 0f, t.forward.z).normalized;
            Vector3 characterRight = new Vector3(t.right.x, 0f, t.right.z).normalized;
            Vector3 directionForward = moveDirection.sqrMagnitude > 0.0001f
                ? new Vector3(moveDirection.x, 0f, moveDirection.z).normalized
                : characterForward;

            cameraForward = cam.GetCameraForwardZeroedYNormalised();
            strafeAngle = characterForward != directionForward
                ? Vector3.SignedAngle(characterForward, directionForward, Vector3.up)
                : 0f;

            float rotSmooth = bodyLocomotion.LocomotionRotationSmoothing;
            float minF = bodyLocomotion.LocomotionForwardStrafeMinThreshold;
            float maxF = bodyLocomotion.LocomotionForwardStrafeMaxThreshold;

            if (isStrafing)
            {
                if (moveDirection.magnitude > 0.01f)
                {
                    if (cameraForward != Vector3.zero)
                    {
                        shuffleDirectionZ = Vector3.Dot(characterForward, directionForward);
                        shuffleDirectionX = Vector3.Dot(characterRight, directionForward);
                        UpdateStrafeDirection(
                            Vector3.Dot(characterForward, directionForward),
                            Vector3.Dot(characterRight, directionForward));
                        cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, 0f, rotSmooth * Time.deltaTime);

                        float targetValue = strafeAngle > minF && strafeAngle < maxF ? 1f : 0f;
                        if (Mathf.Abs(forwardStrafe - targetValue) <= 0.001f)
                            forwardStrafe = targetValue;
                        else
                        {
                            float tt = Mathf.Clamp01(StrafeDirectionDampTime * Time.deltaTime);
                            forwardStrafe = Mathf.SmoothStep(forwardStrafe, targetValue, tt);
                        }
                    }
                }
                else
                {
                    UpdateStrafeDirection(1f, 0f);
                    shuffleDirectionZ = 1f;
                    shuffleDirectionX = 0f;

                    float tt = 20f * Time.deltaTime;
                    float newOffset = 0f;
                    if (characterForward != cameraForward)
                        newOffset = Vector3.SignedAngle(characterForward, cameraForward, Vector3.up);
                    cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, newOffset, tt);
                }
            }
            else
            {
                UpdateStrafeDirection(1f, 0f);
                cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, 0f, rotSmooth * Time.deltaTime);
                shuffleDirectionZ = 1f;
                shuffleDirectionX = 0f;
            }
        }

        private void UpdateStrafeDirection(float targetZ, float targetX)
        {
            strafeDirectionZ = Mathf.Lerp(strafeDirectionZ, targetZ, AnimationDampTime * Time.deltaTime);
            strafeDirectionX = Mathf.Lerp(strafeDirectionX, targetX, AnimationDampTime * Time.deltaTime);
            strafeDirectionZ = Mathf.Round(strafeDirectionZ * 1000f) / 1000f;
            strafeDirectionX = Mathf.Round(strafeDirectionX * 1000f) / 1000f;
        }

        private void CalculateGait(float speed2D)
        {
            float walk = bodyLocomotion.LocomotionWalkSpeed;
            float run = bodyLocomotion.LocomotionRunSpeed;
            float sprint = bodyLocomotion.LocomotionSprintSpeed;
            float runThreshold = (walk + run) / 2f;
            float sprintThreshold = (run + sprint) / 2f;

            if (speed2D < 0.01f)
                currentGait = GaitState.Idle;
            else if (speed2D < runThreshold)
                currentGait = GaitState.Walk;
            else if (speed2D < sprintThreshold)
                currentGait = GaitState.Run;
            else
                currentGait = GaitState.Sprint;
        }
    }
}
