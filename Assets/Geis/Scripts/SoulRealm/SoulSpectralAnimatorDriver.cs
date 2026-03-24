using Geis.InputSystem;
using Geis.Locomotion;
using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Drives the spectral copy's Animator with the same locomotion parameters as
    /// <see cref="GeisPlayerAnimationController"/> (mirror of <c>UpdateAnimatorController</c> + facing/strafe).
    /// </summary>
    public sealed class SoulSpectralAnimatorDriver : MonoBehaviour
    {
        private const float AnimationDampTime = 5f;
        private const float StrafeDirectionDampTime = 20f;

        private static readonly int MovementInputTappedHash = Animator.StringToHash("MovementInputTapped");
        private static readonly int MovementInputPressedHash = Animator.StringToHash("MovementInputPressed");
        private static readonly int MovementInputHeldHash = Animator.StringToHash("MovementInputHeld");
        private static readonly int ShuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
        private static readonly int ShuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int CurrentGaitHash = Animator.StringToHash("CurrentGait");
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int FallingDurationHash = Animator.StringToHash("FallingDuration");
        private static readonly int InclineAngleHash = Animator.StringToHash("InclineAngle");
        private static readonly int StrafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private static readonly int StrafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");
        private static readonly int ForwardStrafeHash = Animator.StringToHash("ForwardStrafe");
        private static readonly int CameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
        private static readonly int IsStrafingHash = Animator.StringToHash("IsStrafing");
        private static readonly int IsTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int IsStoppedHash = Animator.StringToHash("IsStopped");
        private static readonly int IsStartingHash = Animator.StringToHash("IsStarting");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int LeanValueHash = Animator.StringToHash("LeanValue");
        private static readonly int HeadLookXHash = Animator.StringToHash("HeadLookX");
        private static readonly int HeadLookYHash = Animator.StringToHash("HeadLookY");
        private static readonly int BodyLookXHash = Animator.StringToHash("BodyLookX");
        private static readonly int BodyLookYHash = Animator.StringToHash("BodyLookY");
        private static readonly int LocomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");

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

            bool isStrafingAnim = bodyLocomotion.LocomotionIsStrafing;
            FaceMoveDirection(cam, isStrafingAnim);
            CalculateGait(speed2D);
            bool isStopped = moveDirection.magnitude < 0.01f && speed2D < 0.5f;

            animator.SetFloat(LeanValueHash, 0f);
            animator.SetFloat(HeadLookXHash, 0f);
            animator.SetFloat(HeadLookYHash, 0f);
            animator.SetFloat(BodyLookXHash, 0f);
            animator.SetFloat(BodyLookYHash, 0f);
            animator.SetFloat(IsStrafingHash, isStrafingAnim ? 1f : 0f);
            animator.SetFloat(InclineAngleHash, 0f);
            animator.SetFloat(MoveSpeedHash, speed2D);
            animator.SetInteger(CurrentGaitHash, (int)currentGait);
            animator.SetFloat(StrafeDirectionXHash, strafeDirectionX);
            animator.SetFloat(StrafeDirectionZHash, strafeDirectionZ);
            animator.SetFloat(ForwardStrafeHash, forwardStrafe);
            animator.SetFloat(CameraRotationOffsetHash, cameraRotationOffset);
            animator.SetBool(MovementInputHeldHash, movementInputHeld);
            animator.SetBool(MovementInputPressedHash, movementInputPressed);
            animator.SetBool(MovementInputTappedHash, movementInputTapped);
            animator.SetFloat(ShuffleDirectionXHash, shuffleDirectionX);
            animator.SetFloat(ShuffleDirectionZHash, shuffleDirectionZ);
            animator.SetBool(IsTurningInPlaceHash, false);
            animator.SetFloat(FallingDurationHash, fallingDuration);
            animator.SetBool(IsGroundedHash, grounded);
            animator.SetBool(IsWalkingHash, bodyLocomotion.LocomotionIsWalking);
            animator.SetBool(IsCrouchingHash, bodyLocomotion.LocomotionIsCrouching);
            animator.SetBool(IsStoppedHash, isStopped);
            animator.SetBool(IsStartingHash, false);
            animator.SetFloat(LocomotionStartDirectionHash, 0f);
            animator.SetBool(IsJumpingHash, !grounded && motor.VerticalVelocity > 0.5f);
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
