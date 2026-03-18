using UnityEngine;

namespace FunderGames.RPG.OpenWorld
{
    /// <summary>
    /// Main player controller for open world RPG gameplay
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -9.81f;
        
        [Header("Camera Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float cameraDistance = 5f;
        [SerializeField] private float cameraHeight = 2f;
        [SerializeField] private float cameraSmoothness = 5f;
        
        [Header("Combat Settings")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackDamage = 25f;
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private LayerMask enemyLayerMask = 1;
        
        // Components
        private CharacterController characterController;
        private Animator animator;
        private Camera playerCamera;
        private WeaponSystem weaponSystem;
        
        // Movement variables
        private Vector3 moveDirection;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isRunning;
        private bool isAttacking;
        private float attackTimer;
        
        // Camera variables
        private float mouseX;
        private float mouseY;
        private Vector3 cameraTargetPosition;
        private Quaternion cameraTargetRotation;
        
        // Input variables
        private Vector2 moveInput;
        private bool jumpInput;
        private bool attackInput;
        private bool runInput;
        
        // Weapon-based multipliers
        private float walkSpeedMultiplier = 1f;
        private float runSpeedMultiplier = 1f;
        private float jumpHeightMultiplier = 1f;
        
        private void Awake()
        {
            // Get components
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            weaponSystem = GetComponent<WeaponSystem>();
            
            // Find or create camera
            SetupCamera();
        }
        
        private void Start()
        {
            // Lock cursor for mouse look
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Initialize camera position
            if (playerCamera != null)
            {
                cameraTargetPosition = playerCamera.transform.localPosition;
                cameraTargetRotation = playerCamera.transform.localRotation;
            }
        }
        
        private void Update()
        {
            HandleInput();
            HandleMouseLook();
            HandleMovement();
            HandleJump();
            HandleAttack();
            UpdateAnimator();
        }
        
        private void HandleInput()
        {
            // Movement input
            moveInput.x = Input.GetAxis("Horizontal");
            moveInput.y = Input.GetAxis("Vertical");
            
            // Jump input
            jumpInput = Input.GetKeyDown(KeyCode.Space);
            
            // Attack input
            attackInput = Input.GetMouseButtonDown(0);
            
            // Run input
            runInput = Input.GetKey(KeyCode.LeftShift);
            
            // Weapon switching
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                if (weaponSystem != null)
                    weaponSystem.NextWeapon();
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                if (weaponSystem != null)
                    weaponSystem.PreviousWeapon();
            }
            
            // Quick weapon selection
            if (Input.GetKeyDown(KeyCode.Alpha1)) weaponSystem?.EquipWeapon(WeaponCategory.NoWeapon);
            if (Input.GetKeyDown(KeyCode.Alpha2)) weaponSystem?.EquipWeapon(WeaponCategory.SwordAndShield);
            if (Input.GetKeyDown(KeyCode.Alpha3)) weaponSystem?.EquipWeapon(WeaponCategory.TwoHandSword);
            if (Input.GetKeyDown(KeyCode.Alpha4)) weaponSystem?.EquipWeapon(WeaponCategory.Spear);
            if (Input.GetKeyDown(KeyCode.Alpha5)) weaponSystem?.EquipWeapon(WeaponCategory.SingleSword);
            if (Input.GetKeyDown(KeyCode.Alpha6)) weaponSystem?.EquipWeapon(WeaponCategory.MagicWand);
            if (Input.GetKeyDown(KeyCode.Alpha7)) weaponSystem?.EquipWeapon(WeaponCategory.DoubleSword);
            if (Input.GetKeyDown(KeyCode.Alpha8)) weaponSystem?.EquipWeapon(WeaponCategory.BowAndArrow);
        }
        
        private void SetupCamera()
        {
            // Try to find existing camera
            playerCamera = GetComponentInChildren<Camera>();
            
            if (playerCamera == null)
            {
                // Create new camera if none exists
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(transform);
                cameraObj.transform.localPosition = new Vector3(0, cameraHeight, -cameraDistance);
                cameraObj.transform.localRotation = Quaternion.identity;
                
                playerCamera = cameraObj.AddComponent<Camera>();
                playerCamera.tag = "MainCamera";
                
                // Add audio listener if none exists
                if (FindObjectOfType<AudioListener>() == null)
                {
                    cameraObj.AddComponent<AudioListener>();
                }
            }
        }
        
        private void HandleMouseLook()
        {
            if (playerCamera == null) return;
            
            // Get mouse input
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            // Rotate player horizontally
            transform.Rotate(Vector3.up * mouseX);
            
            // Rotate camera vertically
            if (playerCamera != null)
            {
                float currentRotationX = playerCamera.transform.localEulerAngles.x;
                if (currentRotationX > 180f) currentRotationX -= 360f;
                
                float newRotationX = Mathf.Clamp(currentRotationX - mouseY, -80f, 80f);
                playerCamera.transform.localRotation = Quaternion.Euler(newRotationX, 0, 0);
            }
        }
        
        private void HandleMovement()
        {
            // Check if grounded
            isGrounded = characterController.isGrounded;
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small negative value to keep grounded
            }
            
            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            
            // Apply gravity to character controller
            characterController.Move(velocity * Time.deltaTime);
        }
        
        private void HandleJump()
        {
            if (jumpInput && isGrounded && !isAttacking)
            {
                // Apply weapon-based jump height multiplier
                float adjustedJumpHeight = jumpHeight * jumpHeightMultiplier;
                velocity.y = Mathf.Sqrt(adjustedJumpHeight * -2f * gravity);
                animator.SetTrigger("Jump");
                jumpInput = false;
            }
        }
        
        private void HandleAttack()
        {
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
            }
            
            if (attackInput && attackTimer <= 0 && !isAttacking)
            {
                StartAttack();
                attackInput = false;
            }
        }
        
        private void StartAttack()
        {
            isAttacking = true;
            attackTimer = attackCooldown;
            animator.SetTrigger("Attack");
            
            // Find and damage enemies in range
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange, enemyLayerMask);
            foreach (var hitCollider in hitColliders)
            {
                var enemyHealth = hitCollider.GetComponent<HealthSystem>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(attackDamage);
                    Debug.Log($"Hit {hitCollider.name} for {attackDamage} damage!");
                }
            }
            
            // Reset attack state after animation
            Invoke(nameof(EndAttack), attackCooldown);
        }
        
        private void EndAttack()
        {
            isAttacking = false;
            animator.SetBool("IsAttacking", false);
        }
        
        private void UpdateAnimator()
        {
            // Calculate movement speed for root motion
            float currentSpeed = characterController.velocity.magnitude;
            
            // Update animator parameters
            animator.SetFloat("Speed", currentSpeed);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsRunning", isRunning);
            animator.SetBool("IsAttacking", isAttacking);
        }
        
        // Root Motion Handler - This is the key for smooth movement!
        private void OnAnimatorMove()
        {
            if (animator == null) return;
            
            // Get root motion from animator
            Vector3 rootMotion = animator.deltaPosition;
            
            // Apply root motion to character controller
            if (characterController != null && characterController.enabled)
            {
                // Apply weapon-based movement multipliers to root motion
                Vector3 adjustedMotion = rootMotion;
                
                if (isRunning)
                {
                    adjustedMotion *= runSpeedMultiplier;
                }
                else
                {
                    adjustedMotion *= walkSpeedMultiplier;
                }
                
                // Only apply horizontal movement from root motion
                Vector3 horizontalMotion = new Vector3(adjustedMotion.x, 0, adjustedMotion.z);
                
                // Apply the adjusted root motion
                characterController.Move(horizontalMotion);
                
                // Update velocity for gravity calculations
                velocity = characterController.velocity;
            }
        }
        
        // Weapon system integration methods
        /// <summary>
        /// Set attack stats from weapon system
        /// </summary>
        public void SetAttackStats(float damage, float range, float cooldown)
        {
            attackDamage = damage;
            attackRange = range;
            attackCooldown = cooldown;
            
            Debug.Log($"Weapon stats updated: Damage={damage}, Range={range}, Cooldown={cooldown}");
        }
        
        /// <summary>
        /// Set movement multipliers from weapon system
        /// </summary>
        public void SetMovementMultipliers(float walkMultiplier, float runMultiplier, float jumpMultiplier)
        {
            walkSpeedMultiplier = walkMultiplier;
            runSpeedMultiplier = runMultiplier;
            jumpHeightMultiplier = jumpMultiplier;
            
            Debug.Log($"Movement multipliers updated: Walk={walkMultiplier}, Run={runMultiplier}, Jump={jumpMultiplier}");
        }
        
        /// <summary>
        /// Get current weapon stats
        /// </summary>
        public (float damage, float range, float cooldown) GetCurrentWeaponStats()
        {
            return (attackDamage, attackRange, attackCooldown);
        }
        
        /// <summary>
        /// Get current movement multipliers
        /// </summary>
        public (float walk, float run, float jump) GetCurrentMovementMultipliers()
        {
            return (walkSpeedMultiplier, runSpeedMultiplier, jumpHeightMultiplier);
        }
        
        // Public methods for external access
        public void TakeDamage(float damage)
        {
            if (animator != null)
            {
                animator.SetTrigger("TakeDamage");
            }
        }
        
        public void Die()
        {
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }
            
            // Disable player input
            this.enabled = false;
        }
        
        // Gizmos for debugging
        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw camera position
            if (playerCamera != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(playerCamera.transform.position, 0.5f);
            }
        }
    }
}
