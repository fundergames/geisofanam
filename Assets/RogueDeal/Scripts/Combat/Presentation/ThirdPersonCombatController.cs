using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Targeting;
using RogueDeal.Combat.Targeting;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Third-person controller for free-flow combat with dashing and attacks using Animator Controller
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CombatExecutor))]
    [RequireComponent(typeof(CombatEntity))]
    public class ThirdPersonCombatController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -9.81f;
        
        [Header("Combat Settings")]
        [SerializeField] private float dashDistance = 5f;
        [SerializeField] private float dashDuration = 0.3f;
        [SerializeField] private LayerMask enemyLayerMask = 1 << 6; // Default enemy layer
        [SerializeField] private bool useManualDashMovement = false; // If true, manually move during dash instead of relying on root motion
        [SerializeField] private float manualDashSpeed = 15f; // Speed for manual dash movement
        [Tooltip("If true, combat uses weapon colliders for hit detection instead of targeting system")]
        [SerializeField] private bool useWeaponColliders = true; // Default to weapon collider mode
        
        [Header("Combat Actions")]
        [Tooltip("Combat actions available to the player. If empty, will try to get from CombatEntity's CombatProfile.")]
        [SerializeField] private CombatAction[] combatActions;
        
        [Header("Action System Settings")]
        [Tooltip("Number of action states in Animator Controller (Action_1, Action_2, etc.). Default: 2")]
        [SerializeField] private int actionStateCount = 2;
        
        [Tooltip("ActionIndex offset. If Action_1 uses index 0, set to 0. If Action_1 uses index 1, set to 1. Default: 0")]
        [SerializeField] private int actionIndexOffset = 0;
        
        [Header("Input Settings")]
        [Tooltip("Assign the CombatInputReader in the scene (e.g. on the same or another GameObject). When unset, will try to find one.")]
        [SerializeField] private CombatInputReader inputProvider;
        [SerializeField] private bool useNewInputSystem = true; // Used only when inputProvider is unset
        
        [Header("Animator Settings")]
        [Tooltip("Default AnimatorController to assign if the Animator doesn't have one. If not set, will try to load from Resources.")]
        [SerializeField] private RuntimeAnimatorController defaultAnimatorController;
        
        // Components
        private CharacterController characterController;
        private Animator animator;
        private CombatExecutor combatExecutor;
        private CombatEntity combatEntity;
        private Camera mainCamera;
        private TargetingManager targetingManager;
        private LockOnIndicator lockOnIndicator;
        private ICombatInputProvider _inputProvider;
        private static bool _loggedInputProviderMissing;

        // Movement state
        private Vector3 moveDirection;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isDashing;
        private bool isAttacking; // Maps to IsAction in animator
        private float dashTimer;
        private float attackStateTimeout = 0f; // Timeout to reset attack state if animation event doesn't fire
        private Vector3 dashDirection;
        
        // Input
        private Vector2 moveInput;
        private bool dashInput;
        private bool attackInput;
        private bool runInput;
        
        // Animator parameter hashes
        private readonly int speedHash = Animator.StringToHash("Speed");
        private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
        private readonly int isRunningHash = Animator.StringToHash("IsRunning");
        private readonly int runHash = Animator.StringToHash("Run"); // Alternative to IsRunning
        private readonly int dashTriggerHash = Animator.StringToHash("Dash");
        private readonly int takeActionTriggerHash = Animator.StringToHash("TakeAction");
        private readonly int actionIndexHash = Animator.StringToHash("ActionIndex");
        private readonly int isActionHash = Animator.StringToHash("IsAction");
        
        // Attack trigger hashes (for controllers that use Attack_1/2/3 directly)
        private readonly int attack1Hash = Animator.StringToHash("Attack_1");
        private readonly int attack2Hash = Animator.StringToHash("Attack_2");
        private readonly int attack3Hash = Animator.StringToHash("Attack_3");
        
        // Parameter detection (set at runtime)
        private bool useLegacyRunParameter = false;
        private bool useLegacyAttackTriggers = false;
        
        // Combat actions
        private CombatAction[] availableActions;
        private int currentComboIndex = 0;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            
            // Get animator - try self first, then children (animator is often on child visual object)
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            // Also try getting from CombatEntity (it also searches children)
            if (animator == null)
            {
                combatEntity = GetComponent<CombatEntity>();
                if (combatEntity != null)
                {
                    animator = combatEntity.animator;
                }
            }
            
            combatExecutor = GetComponent<CombatExecutor>();
            if (combatEntity == null)
            {
                combatEntity = GetComponent<CombatEntity>();
            }
            
            // Get or add TargetingManager
            targetingManager = GetComponent<TargetingManager>();
            if (targetingManager == null)
            {
                targetingManager = gameObject.AddComponent<TargetingManager>();
                Debug.Log("[ThirdPersonCombatController] Added TargetingManager component");
            }
            
            // Get or add LockOnIndicator
            lockOnIndicator = GetComponentInChildren<LockOnIndicator>();
            if (lockOnIndicator == null)
            {
                GameObject indicatorObj = new GameObject("LockOnIndicator");
                indicatorObj.transform.SetParent(transform);
                indicatorObj.transform.localPosition = Vector3.zero;
                lockOnIndicator = indicatorObj.AddComponent<LockOnIndicator>();
                Debug.Log("[ThirdPersonCombatController] Created LockOnIndicator");
            }
            
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();

            _inputProvider = inputProvider as ICombatInputProvider;
            if (_inputProvider == null && inputProvider != null)
                _inputProvider = inputProvider.GetComponent<ICombatInputProvider>();
            if (_inputProvider == null)
                _inputProvider = FindObjectOfType<CombatInputReader>();
            
            if (mainCamera == null)
            {
                Debug.LogWarning("[ThirdPersonCombatController] No camera found! Movement will use world space. Make sure there's a camera tagged 'MainCamera' in the scene.");
            }
            else
            {
                Debug.Log($"[ThirdPersonCombatController] Using camera: {mainCamera.name}");
            }
            
            // Ensure root motion is enabled
            if (animator != null)
            {
                animator.applyRootMotion = true;
                Debug.Log($"[ThirdPersonCombatController] Found animator on: {animator.gameObject.name}");
                
                // Check if animator has a runtime controller assigned
                if (animator.runtimeAnimatorController == null)
                {
                    // Try to auto-assign a controller
                    RuntimeAnimatorController controllerToAssign = null;
                    
                    // First, try the assigned default controller
                    if (defaultAnimatorController != null)
                    {
                        controllerToAssign = defaultAnimatorController;
                        Debug.Log($"[ThirdPersonCombatController] Using assigned default AnimatorController: {defaultAnimatorController.name}");
                    }
                    // If no default assigned, try to load from Resources
                    else
                    {
                        // Try common resource paths
                        string[] resourcePaths = new string[]
                        {
                            "Combat/Animations/ThirdPerson_Controller",
                            "RogueDeal/Combat/Animations/ThirdPerson_Controller",
                            "Animations/ThirdPerson_Controller"
                        };
                        
                        foreach (string path in resourcePaths)
                        {
                            RuntimeAnimatorController loadedController = Resources.Load<RuntimeAnimatorController>(path);
                            if (loadedController != null)
                            {
                                controllerToAssign = loadedController;
                                Debug.Log($"[ThirdPersonCombatController] Loaded AnimatorController from Resources: {path}");
                                break;
                            }
                        }
                    }
                    
                    // Assign the controller if we found one
                    if (controllerToAssign != null)
                    {
                        animator.runtimeAnimatorController = controllerToAssign;
                        Debug.Log($"[ThirdPersonCombatController] ✅ Auto-assigned AnimatorController: {controllerToAssign.name}");
                    }
                    else
                    {
                        Debug.LogError($"[ThirdPersonCombatController] ❌ Animator found on '{animator.gameObject.name}' but no AnimatorController is assigned! " +
                                     $"Please assign an AnimatorController in the Inspector or set the 'Default Animator Controller' field. " +
                                     $"The animator will not work without a controller.");
                    }
                }
                else
                {
                    Debug.Log($"[ThirdPersonCombatController] ✅ AnimatorController assigned: {animator.runtimeAnimatorController.name}");
                }
                
                // Detect which parameter system is available
                DetectAnimatorParameters();
            }
            else
            {
                Debug.LogError("[ThirdPersonCombatController] No animator found on self or children! " +
                             $"Searched: {gameObject.name} and all children. " +
                             $"Make sure an Animator component exists on this GameObject or a child GameObject.");
            }
            
            // Get combat actions
            InitializeCombatActions();
        }
        
        private void DetectAnimatorParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            
            // Check if controller uses Run (legacy) instead of IsRunning
            useLegacyRunParameter = HasParameter("Run") && !HasParameter("IsRunning");
            
            // Check if controller uses Attack_1/2/3 triggers directly (legacy)
            useLegacyAttackTriggers = HasParameter("Attack_1") && HasParameter("Attack_2");
            
            if (useLegacyRunParameter)
            {
                Debug.Log("[ThirdPersonCombatController] Using legacy 'Run' parameter instead of 'IsRunning'");
            }
            
            if (useLegacyAttackTriggers)
            {
                Debug.Log("[ThirdPersonCombatController] Using legacy Attack_1/2/3 triggers instead of TakeAction+ActionIndex");
            }
        }
        
        /// <summary>
        /// Checks if the animator is valid (not null and has a runtime controller assigned)
        /// </summary>
        private bool IsAnimatorValid()
        {
            if (animator == null)
            {
                return false;
            }
            
            if (animator.runtimeAnimatorController == null)
            {
                return false;
            }
            
            return true;
        }
        
        private bool HasParameter(string paramName)
        {
            if (!IsAnimatorValid()) return false;
            
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
        }
        
        /// <summary>
        /// Gets a string listing all animator parameters for debugging
        /// </summary>
        private string GetAnimatorParametersString()
        {
            if (!IsAnimatorValid()) return "Animator not valid";
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{param.name} ({param.type})");
            }
            return sb.Length > 0 ? sb.ToString() : "No parameters";
        }
        
        private void InitializeCombatActions()
        {
            // Use assigned actions if available
            if (combatActions != null && combatActions.Length > 0)
            {
                availableActions = combatActions;
                Debug.Log($"[ThirdPersonCombatController] Using {availableActions.Length} assigned combat actions");
            }
            // Otherwise try to get from CombatExecutor (which has access to entityData)
            else if (combatExecutor != null)
            {
                // CombatExecutor has access to entityData through its internal system
                // For now, we'll require actions to be assigned in inspector
                Debug.LogWarning("[ThirdPersonCombatController] No combat actions assigned. Please assign CombatAction assets in the inspector.");
            }
            else
            {
                Debug.LogWarning("[ThirdPersonCombatController] No combat actions available. Please assign CombatAction assets in the inspector.");
            }
        }
        
        private void Update()
        {
            HandleInput();
            HandleMovement();
            UpdateAnimator();
            UpdateTimers();
            CheckAttackState(); // Check if attack state should be reset
            UpdateLockOnIndicator(); // Update lock-on indicator position
        }
        
        private void UpdateLockOnIndicator()
        {
            if (lockOnIndicator == null || targetingManager == null)
            {
                if (lockOnIndicator == null)
                {
                    Debug.LogWarning("[ThirdPersonCombatController] LockOnIndicator is null!");
                }
                if (targetingManager == null)
                {
                    Debug.LogWarning("[ThirdPersonCombatController] TargetingManager is null!");
                }
                return;
            }
            
            // Check for locked-on target first (click-to-select)
            var lockedTarget = targetingManager.GetLockedOnTarget();
            if (lockedTarget != null)
            {
                lockOnIndicator.SetTarget(lockedTarget, true);
                return;
            }
            
            // For single target strategies, continuously resolve targets to detect changes
            var currentStrategy = targetingManager.GetCurrentStrategy();
            if (currentStrategy == null)
            {
                // No strategy set - clear indicator
                Debug.LogWarning("[ThirdPersonCombatController] No targeting strategy is set!");
                lockOnIndicator.ClearTarget();
                return;
            }
            
            // Debug.Log($"[ThirdPersonCombatController] Current strategy: {currentStrategy.GetType().Name}");
            
            if (currentStrategy is SingleTargetSelector)
            {
                // Get a default action to use for targeting (or use null if we just need the strategy)
                CombatAction actionForTargeting = null;
                if (availableActions != null && availableActions.Length > 0)
                {
                    actionForTargeting = availableActions[0]; // Use first action for targeting resolution
                }
                
                // Try to resolve targets - use action if available, otherwise use strategy directly
                TargetResult targetResult = null;
                
                if (actionForTargeting != null)
                {
                    // Debug.Log($"[ThirdPersonCombatController] Using action '{actionForTargeting.actionName}' for targeting");
                    targetResult = targetingManager.GetTargets(actionForTargeting);
                }
                else if (combatEntity != null)
                {
                    // No action available, try to resolve using just the strategy
                    var attackerData = combatEntity.GetEntityData();
                    if (attackerData != null)
                    {
                        Debug.Log("[ThirdPersonCombatController] No action available, using strategy directly");
                        targetResult = currentStrategy.ResolveTargets(attackerData);
                    }
                    else
                    {
                        Debug.LogWarning("[ThirdPersonCombatController] CombatEntity has no EntityData!");
                    }
                }
                else
                {
                    Debug.LogWarning("[ThirdPersonCombatController] CombatEntity is null!");
                }
                
                // Update indicator based on result
                if (targetResult != null && targetResult.isReady && targetResult.targets != null && targetResult.targets.Count > 0)
                {
                    var currentTarget = targetResult.targets[0];
                    if (currentTarget != null)
                    {
                        // Debug.Log($"[ThirdPersonCombatController] ✅ Setting lock-on indicator to: {currentTarget.name}");
                        lockOnIndicator.SetTarget(currentTarget, true);
                    }
                    else
                    {
                        // Debug.LogWarning("[ThirdPersonCombatController] Target result has null target!");
                        lockOnIndicator.ClearTarget();
                    }
                }
                else
                {
                    // No valid target in range
                    if (targetResult == null)
                    {
                        Debug.LogWarning("[ThirdPersonCombatController] TargetResult is null!");
                    }
                    else if (!targetResult.isReady)
                    {
                        // Debug.LogWarning("[ThirdPersonCombatController] TargetResult is not ready!");
                    }
                    else if (targetResult.targets == null || targetResult.targets.Count == 0)
                    {
                        // Debug.LogWarning("[ThirdPersonCombatController] TargetResult has no targets!");
                    }
                    lockOnIndicator.ClearTarget();
                }
            }
            else
            {
                // Not a single target strategy - clear indicator
                Debug.Log($"[ThirdPersonCombatController] Current strategy is not SingleTargetSelector, it's {currentStrategy.GetType().Name}");
                lockOnIndicator.ClearTarget();
            }
        }
        
        private void HandleInput()
        {
            moveInput = Vector2.zero;
            runInput = false;
            dashInput = false;
            attackInput = false;

            if (_inputProvider == null)
            {
                _inputProvider = inputProvider;
                if (_inputProvider == null)
                    _inputProvider = FindObjectOfType<CombatInputReader>();
                if (_inputProvider == null && !_loggedInputProviderMissing)
                {
                    _loggedInputProviderMissing = true;
                    Debug.LogWarning("[ThirdPersonCombatController] No CombatInputReader found. Add one to the scene, or assign Input Provider in the inspector. Using fallback input.");
                }
            }

            if (_inputProvider != null)
            {
                CombatInputState state = _inputProvider.GetState();
                moveInput = state.Move;
                runInput = state.Run;
                dashInput = state.DashPressed;
                attackInput = state.AttackPressed;
                if (state.AttackPressed && state.HasAttackClickPosition && targetingManager != null)
                    targetingManager.HandleMouseClick(state.AttackClickScreenPosition);
                return;
            }

            // Fallback: read directly when no input provider is set
            if (useNewInputSystem)
            {
                var keyboard = Keyboard.current;
                var mouse = Mouse.current;
                if (keyboard != null)
                {
                    if (keyboard.wKey.isPressed) moveInput.y += 1f;
                    if (keyboard.sKey.isPressed) moveInput.y -= 1f;
                    if (keyboard.aKey.isPressed) moveInput.x -= 1f;
                    if (keyboard.dKey.isPressed) moveInput.x += 1f;
                    runInput = keyboard.leftShiftKey.isPressed;
                    dashInput = keyboard.spaceKey.wasPressedThisFrame;
                }
                if (mouse != null)
                {
                    attackInput = mouse.leftButton.wasPressedThisFrame;
                    if (attackInput && targetingManager != null)
                        targetingManager.HandleMouseClick(mouse.position.ReadValue());
                }
                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    Vector2 stick = gamepad.leftStick.ReadValue();
                    if (stick.sqrMagnitude > 0.01f) moveInput = stick;
                    runInput = runInput || gamepad.leftStickButton.isPressed;
                    dashInput = dashInput || gamepad.buttonSouth.wasPressedThisFrame;
                    attackInput = attackInput || gamepad.buttonWest.wasPressedThisFrame || gamepad.rightTrigger.wasPressedThisFrame;
                }
                return;
            }

            try
            {
                moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
                runInput = Input.GetKey(KeyCode.LeftShift);
                dashInput = Input.GetKeyDown(KeyCode.Space);
                attackInput = Input.GetMouseButtonDown(0);
                if (attackInput && targetingManager != null)
                    targetingManager.HandleMouseClick(Input.mousePosition);
            }
            catch (System.InvalidOperationException)
            {
                useNewInputSystem = true;
                Debug.Log("[ThirdPersonCombatController] Legacy Input not available - switched to New Input System");
            }
        }
        
        private void HandleMovement()
        {
            isGrounded = characterController.isGrounded;
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            
            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            
            // Handle dashing
            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0)
                {
                    isDashing = false;
                    
                    // Dash just ended - apply gravity
                    if (characterController != null && characterController.enabled)
                    {
                        characterController.Move(velocity * Time.deltaTime);
                    }
                }
                else
                {
                    // Always use manual dash movement (either as primary or fallback)
                    // This ensures dash moves even if animation doesn't have root motion enabled
                    if (useManualDashMovement && characterController != null && characterController.enabled)
                    {
                        Vector3 dashMovement = dashDirection * manualDashSpeed * Time.deltaTime;
                        
                        // Combine horizontal dash movement with vertical velocity (gravity)
                        Vector3 combinedMovement = dashMovement + (velocity * Time.deltaTime);
                        
                        // Apply combined movement (horizontal dash + vertical gravity)
                        characterController.Move(combinedMovement);
                    }
                    else if (!useManualDashMovement)
                    {
                        // Manual dash not enabled - rely on root motion only
                        
                        // Still apply gravity
                        if (characterController != null && characterController.enabled)
                        {
                            characterController.Move(velocity * Time.deltaTime);
                        }
                    }
                    
                    // Note: Root motion from animation will ALSO be applied in OnAnimatorMove if available
                    // Manual movement acts as primary or fallback
                }
                
                // Skip normal movement during dash (movement already applied above)
                return;
            }
            
            // Handle attacking (limited movement)
            if (isAttacking)
            {
                // Limited movement during attack (uses root motion from animation)
                // Animation will handle the movement via root motion
                // Skip normal movement but still apply gravity at the end
            }
            // Normal movement (only if not dashing or attacking)
            else if (!isDashing && !isAttacking && moveInput.magnitude > 0.1f)
            {
                // Calculate move direction relative to camera
                if (mainCamera == null)
                {
                    // Fallback: use world space if no camera
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.LogWarning("[ThirdPersonCombatController] Main camera is null! Using world space movement.");
                    }
                    moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                }
                else
                {
                    // Camera-relative movement
                    Vector3 cameraForward = mainCamera.transform.forward;
                    Vector3 cameraRight = mainCamera.transform.right;
                    
                    // Flatten to horizontal plane
                    cameraForward.y = 0f;
                    cameraRight.y = 0f;
                    cameraForward.Normalize();
                    cameraRight.Normalize();
                    
                    // Calculate movement direction (God of War style: W = forward into screen, A/D = strafe)
                    // moveInput.y is forward/back (W/S), moveInput.x is left/right (A/D)
                    moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
                }

                // Rotate character to face movement direction (mouse rotates only the camera)
                if (moveDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                // Calculate speed
                float currentSpeed = runInput ? runSpeed : walkSpeed;
                
                // Calculate movement vector - move in the intended direction
                Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
                
                // Move character
                if (characterController != null && characterController.enabled)
                {
                    characterController.Move(movement);
                }
            }
            else
            {
                // No input - reset move direction
                moveDirection = Vector3.zero;
            }
            
            // Apply gravity
            characterController.Move(velocity * Time.deltaTime);
            
            // Handle dash input
            if (dashInput && !isDashing && !isAttacking && isGrounded)
            {
                StartDash();
            }
            
            // Handle attack input
            if (attackInput && !isAttacking && !isDashing)
            {
                StartAttack();
                // Clear attack input after processing to prevent double-triggering
                // This ensures the input is only processed once per press
                attackInput = false;
            }
        }
        
        private void StartDash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            
            // Determine dash direction
            if (moveInput.magnitude > 0.1f)
            {
                // Dash in movement direction
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;
                
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();
                
                dashDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
            }
            else
            {
                // Dash forward
                dashDirection = transform.forward;
            }
            
            // Rotate character towards dash direction
            transform.rotation = Quaternion.LookRotation(dashDirection);
            
            // Trigger dash animation
            if (animator != null && IsAnimatorValid())
            {
                // Ensure root motion is enabled for dash
                if (!animator.applyRootMotion)
                {
                    Debug.LogWarning("[ThirdPersonCombatController] Root motion is disabled! Enabling for dash.");
                    animator.applyRootMotion = true;
                }
                
                // Set Speed to 0 to prevent unwanted transitions
                if (HasParameter("Speed"))
                {
                    animator.SetFloat(speedHash, 0f);
                }
                
                // Clear running state
                if (useLegacyRunParameter && HasParameter("Run"))
                {
                    animator.SetBool(runHash, false);
                }
                else if (HasParameter("IsRunning"))
                {
                    animator.SetBool(isRunningHash, false);
                }
                
                if (HasParameter("Dash"))
                {
                    animator.SetTrigger(dashTriggerHash);
                }
                else
                {
                    // Force manual dash movement if no Dash parameter
                    useManualDashMovement = true;
                }
                
                // Start coroutine to check dash animation info
                StartCoroutine(CheckDashAnimationInfo());
            }
            else
            {
                // No animator or invalid - use manual dash movement
                useManualDashMovement = true;
            }
        }
        
        /// <summary>
        /// Checks dash animation info to diagnose root motion issues
        /// </summary>
        private IEnumerator CheckDashAnimationInfo()
        {
            // Wait a frame for animation to start
            yield return null;
            
            if (isDashing)
            {
                // Check if animator is valid (not null and has controller)
                if (!IsAnimatorValid())
                {
                    yield break;
                }
                
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                
                if (clipInfo != null && clipInfo.Length > 0)
                {
                    AnimationClip clip = clipInfo[0].clip;
                    
                    // Check if animation is "InPlace" - these don't have root motion
                    bool isInPlace = clip.name.Contains("InPlace") || clip.name.Contains("In_Place");
                    
                    // Also check if root motion is actually working
                    // If OnAnimatorMove reports zero deltaPosition, the animation likely has no root motion
                    if (isInPlace)
                    {
                        useManualDashMovement = true;
                    }
                }
            }
        }
        
        private void StartAttack()
        {
            // Set attack state IMMEDIATELY to prevent double-triggering
            // This must be done first, before any other logic
            if (isAttacking)
            {
                return; // Already attacking, prevent re-entry
            }
            
            isAttacking = true;
            
            // Calculate action index for animator
            int actionCount = availableActions != null && availableActions.Length > 0 
                ? availableActions.Length 
                : actionStateCount;
            int actionIndex = (currentComboIndex % actionCount) + actionIndexOffset;
            CombatAction actionToUse = null;
            
            // Get the action we're trying to use
            if (availableActions != null && availableActions.Length > 0)
            {
                actionToUse = availableActions[currentComboIndex % actionCount];
                
                    // Check if action is available (cooldown check)
                    if (combatExecutor != null)
                    {
                        var cooldownManager = combatExecutor.GetCooldownManager();
                        if (cooldownManager != null && !cooldownManager.IsActionAvailable(actionToUse))
                        {
                            // Reset attack state since we can't attack
                            isAttacking = false;
                            return; // Don't start attack if on cooldown
                        }
                    }
            }
            
            // Get target using TargetingManager
            CombatEntity target = null;
            Vector3 targetPosition = transform.position;
            
            if (targetingManager != null && actionToUse != null)
            {
                var targetResult = targetingManager.GetTargets(actionToUse);
                if (targetResult != null && targetResult.isReady && targetResult.targets != null && targetResult.targets.Count > 0)
                {
                    target = targetResult.targets[0]; // Get first target
                    targetPosition = targetResult.targetPosition;
                }
            }
            else
            {
                // Fallback to old method if no TargetingManager
                target = FindNearestTarget();
                if (target != null)
                {
                    targetPosition = target.transform.position;
                }
            }
            
            // Update lock-on indicator
            if (lockOnIndicator != null)
            {
                if (target != null && targetingManager != null && targetingManager.IsLockedOn())
                {
                    lockOnIndicator.SetTarget(target, true);
                }
                else
                {
                    lockOnIndicator.ClearTarget();
                }
            }
            
            // Rotate towards target if we have one
            if (target != null)
            {
                Vector3 directionToTarget = (targetPosition - transform.position);
                directionToTarget.y = 0f;
                if (directionToTarget.magnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(directionToTarget.normalized);
                }
            }
            // For directional targeting, rotate towards movement direction if moving
            else if (moveInput.magnitude > 0.1f)
            {
                // Directional targeting - rotate towards movement direction
                Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
                if (mainCamera != null)
                {
                    Vector3 cameraForward = mainCamera.transform.forward;
                    Vector3 cameraRight = mainCamera.transform.right;
                    cameraForward.y = 0f;
                    cameraRight.y = 0f;
                    cameraForward.Normalize();
                    cameraRight.Normalize();
                    moveDir = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
                }
                if (moveDir.magnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(moveDir);
                }
            }
            
            // Always trigger the animation first (even if no targets)
            if (animator != null)
            {
                // Check if animator is valid (has controller)
                if (!IsAnimatorValid())
                {
                    Debug.LogWarning("[ThirdPersonCombatController] Animator is not valid (null or no controller)! Cannot start attack.");
                    isAttacking = false;
                    return;
                }
                
                // Check if we're already in an action state to prevent double-triggering
                AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
                bool isInActionState = (HasParameter("IsAction") && animator.GetBool(isActionHash)) || 
                                       currentState.IsName("Action_1") || 
                                       currentState.IsName("Action_2") || 
                                       currentState.IsName("Attack_1") ||
                                       currentState.IsName("Attack_2") ||
                                       currentState.IsName("Attack_3");
                
                if (isInActionState)
                {
                    return;
                }
                
                // CRITICAL: Set Speed to 0 BEFORE setting the trigger to prevent Walk → Idle → Action
                // This ensures direct transitions from Walk/Run to Action states
                if (HasParameter("Speed"))
                {
                    animator.SetFloat(speedHash, 0f);
                }
                
                // Clear running state
                if (useLegacyRunParameter && HasParameter("Run"))
                {
                    animator.SetBool(runHash, false);
                }
                else if (HasParameter("IsRunning"))
                {
                    animator.SetBool(isRunningHash, false);
                }
                
                // Use Attack_1/2/3 triggers if available (legacy system)
                if (useLegacyAttackTriggers)
                {
                    // Determine which attack trigger to use (0=Attack_1, 1=Attack_2, 2=Attack_3)
                    int attackNumber = (currentComboIndex % actionCount) + 1; // 1, 2, or 3
                    
                    if (attackNumber == 1 && HasParameter("Attack_1"))
                    {
                        animator.SetTrigger(attack1Hash);
                    }
                    else if (attackNumber == 2 && HasParameter("Attack_2"))
                    {
                        animator.SetTrigger(attack2Hash);
                    }
                    else if (attackNumber >= 3 && HasParameter("Attack_3"))
                    {
                        animator.SetTrigger(attack3Hash);
                    }
                }
                else
                {
                    // Use new system (TakeAction + ActionIndex)
                    if (HasParameter("ActionIndex"))
                    {
                        animator.SetInteger(actionIndexHash, actionIndex);
                    }
                    if (HasParameter("IsAction"))
                    {
                        animator.SetBool(isActionHash, true);
                    }
                    if (HasParameter("TakeAction"))
                    {
                        animator.SetTrigger(takeActionTriggerHash);
                    }
                }
                
                // Set timeout based on animation length (will be updated next frame when animation starts)
                // For now, set a default, then update it next frame
                attackStateTimeout = 5f; // Default fallback
                StartCoroutine(UpdateAttackTimeoutFromAnimation());
            }
            else
            {
                // No animator - use default timeout
                attackStateTimeout = 5f;
            }
            
            // Set up combat action - either use weapon colliders or targeting system
            if (actionToUse != null && combatExecutor != null)
            {
                if (useWeaponColliders)
                {
                    // Weapon collider mode: Set the current action so WeaponHitbox can use it
                    // The WeaponHitbox will handle damage application when it detects collisions
                    combatExecutor.SetCurrentAction(actionToUse);
                    
                    // Increment combo index for next attack
                    currentComboIndex++;
                    if (currentComboIndex >= actionCount)
                    {
                        currentComboIndex = 0;
                    }
                }
                else
                {
                    // Legacy targeting mode: Execute action with targeting system
                    bool executed = combatExecutor.ExecuteAction(actionToUse);
                    
                    // Increment combo index for next attack
                    currentComboIndex++;
                    if (currentComboIndex >= actionCount)
                    {
                        currentComboIndex = 0;
                    }
                }
            }
            else
            {
                // No combat actions assigned - just play animation
                // Increment combo index for next attack
                currentComboIndex++;
                if (currentComboIndex >= actionCount)
                {
                    currentComboIndex = 0;
                }
            }
        }
        
        private CombatEntity FindNearestTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 10f, enemyLayerMask);
            
            CombatEntity nearestTarget = null;
            float nearestDistance = float.MaxValue;
            
            foreach (Collider col in colliders)
            {
                CombatEntity entity = col.GetComponent<CombatEntity>();
                if (entity != null && entity != combatEntity)
                {
                    float distance = Vector3.Distance(transform.position, entity.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTarget = entity;
                    }
                }
            }
            
            return nearestTarget;
        }
        
        private void UpdateAnimator()
        {
            if (animator == null) return;
            
            // Check if animator is valid
            if (!IsAnimatorValid())
            {
                return;
            }
            
            // Don't update movement parameters during attacks - let the action animation play
            // This prevents transitions from Action → Idle → Action
            if (!isAttacking && !isDashing)
            {
                // Update speed parameter
                // Try normalized first: walk = 0.5, run = 1.0 (many animators expect 0-1 range)
                if (moveInput.magnitude > 0.1f)
                {
                    float normalizedSpeed = runInput ? 1.0f : 0.5f;
                    animator.SetFloat(speedHash, normalizedSpeed);
                }
                else
                {
                    // No input - set speed to 0
                    animator.SetFloat(speedHash, 0f);
                }
                
                // Update running state - use Run if available, otherwise IsRunning
                bool isRunning = runInput && moveInput.magnitude > 0.1f;
                if (useLegacyRunParameter && HasParameter("Run"))
                {
                    animator.SetBool(runHash, isRunning);
                }
                else if (HasParameter("IsRunning"))
                {
                    animator.SetBool(isRunningHash, isRunning);
                }
            }
            // During attacks/dashes, keep speed at 0 to prevent unwanted transitions
            else
            {
                if (HasParameter("Speed"))
                {
                    animator.SetFloat(speedHash, 0f);
                }
                
                if (useLegacyRunParameter && HasParameter("Run"))
                {
                    animator.SetBool(runHash, false);
                }
                else if (HasParameter("IsRunning"))
                {
                    animator.SetBool(isRunningHash, false);
                }
            }
            
            // Always update grounded state (independent of attack state) - only if parameter exists
            if (HasParameter("IsGrounded"))
            {
                animator.SetBool(isGroundedHash, isGrounded);
            }
        }
        
        private void UpdateTimers()
        {
            // Attack cooldown is now handled by CombatExecutor's ActionCooldownManager
            // No need for a separate attack timer
        }
        
        private void CheckAttackState()
        {
            // If we're in an attack state, check if we should reset it
            if (isAttacking)
            {
                // Decrement timeout
                if (attackStateTimeout > 0)
                {
                    attackStateTimeout -= Time.deltaTime;
                    if (attackStateTimeout <= 0)
                    {
                        // Timeout reached - force reset attack state
                        ResetAttackState();
                    }
                }
                
                // Also check animator state - if IsAction is false, we're no longer in an action state
                // (Only if using new system with IsAction parameter)
                if (animator != null && !useLegacyAttackTriggers && HasParameter("IsAction"))
                {
                    bool isAction = animator.GetBool(isActionHash);
                    if (!isAction)
                    {
                        // Animator says we're not in action state, but our flag says we are
                        // This means the animator transitioned out but the event didn't fire
                        ResetAttackState();
                    }
                }
            }
        }
        
        private void ResetAttackState()
        {
            isAttacking = false;
            attackStateTimeout = 0f;
            
            // Clear current action in CombatExecutor when attack completes (for weapon collider mode)
            if (useWeaponColliders && combatExecutor != null)
            {
                combatExecutor.ClearCurrentAction();
            }
            
            if (animator != null && !useLegacyAttackTriggers && HasParameter("IsAction"))
            {
                animator.SetBool(isActionHash, false);
            }
        }
        
        /// <summary>
        /// Updates the attack timeout based on the actual animation length.
        /// Waits a frame for the animation to start, then gets its length.
        /// </summary>
        private IEnumerator UpdateAttackTimeoutFromAnimation()
        {
            // Wait a frame for the animation to start playing
            yield return null;
            
            if (isAttacking)
            {
                // Check if animator is valid (not null and has controller)
                if (!IsAnimatorValid())
                {
                    attackStateTimeout = 5f;
                    yield break;
                }
                
                // Try to get the clip info first (most reliable)
                AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo != null && clipInfo.Length > 0)
                {
                    // Get the length of the actual animation clip
                    float clipLength = clipInfo[0].clip.length;
                    // Account for animator speed multiplier
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    float speedMultiplier = stateInfo.speed;
                    float actualLength = clipLength / Mathf.Max(speedMultiplier, 0.01f); // Avoid division by zero
                    
                    // Set timeout to animation length + small buffer (0.2s) to account for transition time
                    attackStateTimeout = actualLength + 0.2f;
                }
                else
                {
                    // Fallback: use state info length
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    if (stateInfo.length > 0)
                    {
                        attackStateTimeout = stateInfo.length + 0.2f;
                    }
                    else
                    {
                        // Final fallback: use default timeout
                        attackStateTimeout = 5f;
                    }
                }
            }
        }
        
        private void OnAnimatorMove()
        {
            if (animator == null)
            {
                return;
            }
            
            if (!animator.applyRootMotion)
            {
                return;
            }
            
            Vector3 rootMotion = animator.deltaPosition;
            
            // Only apply root motion during dash or attack animations
            // During normal movement, we handle movement manually in HandleMovement()
            if (!isDashing && !isAttacking)
            {
                // During normal movement, ignore root motion (we handle it manually)
                // This prevents idle/walk/run animations from interfering with player-controlled movement
                // DO NOT apply root motion here - just return
                return;
            }
            
            // Apply root motion from animations (only for dash/attack)
            if (characterController != null && characterController.enabled)
            {
                if (rootMotion.magnitude > 0.001f)
                {
                    // Apply root motion
                    characterController.Move(rootMotion);
                    
                    // Apply rotation from root motion if any
                    if (animator.deltaRotation != Quaternion.identity)
                    {
                        transform.rotation = transform.rotation * animator.deltaRotation;
                    }
                }
                else if (isDashing)
                {
                    // No root motion detected during dash - enable manual dash movement as fallback
                    if (!useManualDashMovement)
                    {
                        useManualDashMovement = true;
                    }
                }
            }
        }
        
        // Animation event callbacks (called from animation events)
        public void OnAttackStart()
        {
            // Called when attack animation starts
        }
        
        public void OnAttackEnd()
        {
            // Called when action animation ends
            ResetAttackState();
        }
        
        public void OnDashStart()
        {
            // Called when dash animation started
        }
        
        public void OnDashEnd()
        {
            // Called when dash animation ends
            isDashing = false;
        }
        
        // Public methods for external control
        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
        }
        
        public void SetDashInput(bool dash)
        {
            dashInput = dash;
        }
        
        public void SetAttackInput(bool attack)
        {
            attackInput = attack;
        }
        
        public void SetRunInput(bool run)
        {
            runInput = run;
        }
        
        public bool IsAttacking => isAttacking;
        public bool IsDashing => isDashing;
        public bool IsGrounded => isGrounded;
    }
}


