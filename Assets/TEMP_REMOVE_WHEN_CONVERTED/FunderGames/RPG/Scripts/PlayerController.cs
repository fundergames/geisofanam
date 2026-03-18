using UnityEngine;

namespace FunderGames.RPG
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerAnimationController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -9.81f;
#pragma warning disable CS0414
        [SerializeField] private float rollSpeed = 10f;
        [SerializeField] private float dashSpeed = 15f;
#pragma warning restore CS0414
        
        [Header("Input Settings")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode runKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode rollKey = KeyCode.Q;
        [SerializeField] private KeyCode dashKey = KeyCode.E;
        [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode defendKey = KeyCode.Mouse1;
        
        private CharacterController characterController;
        private PlayerAnimationController animationController;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isRolling;
        private bool isDashing;
        private bool isAttacking;
        private bool isDefending;
        
        // Movement state
        private float currentMoveSpeed;
        private Vector3 moveDirection;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animationController = GetComponent<PlayerAnimationController>();
            currentMoveSpeed = moveSpeed;
        }
        
        private void Update()
        {
            HandleInput();
            HandleMovement();
            HandleAnimations();
        }
        
        private void HandleInput()
        {
            // Get input axes
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            // Debug input values
            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                Debug.Log($"Input - Horizontal: {horizontal}, Vertical: {vertical}");
            }
            
            // Calculate move direction
            moveDirection = new Vector3(horizontal, 0, vertical).normalized;
            
            // Handle running
            if (Input.GetKey(runKey) && moveDirection.magnitude > 0.1f)
            {
                currentMoveSpeed = runSpeed;
                Debug.Log("Running mode activated");
            }
            else
            {
                currentMoveSpeed = moveSpeed;
            }
            
            // Handle jumping
            if (Input.GetKeyDown(jumpKey) && isGrounded && !isRolling && !isDashing)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animationController.TriggerJump();
                Debug.Log("Jump triggered");
            }
            
            // Handle rolling
            if (Input.GetKeyDown(rollKey) && isGrounded && !isRolling && !isDashing && !isAttacking)
            {
                StartRoll();
                Debug.Log("Roll triggered");
            }
            
            // Handle dashing
            if (Input.GetKeyDown(dashKey) && isGrounded && !isRolling && !isDashing && !isAttacking)
            {
                StartDash();
                Debug.Log("Dash triggered");
            }
            
            // Handle attacking
            if (Input.GetKeyDown(attackKey) && !isRolling && !isDashing && !isAttacking)
            {
                StartAttack();
                Debug.Log("Attack triggered");
            }
            
            // Handle defending
            if (Input.GetKey(defendKey) && !isRolling && !isDashing && !isAttacking)
            {
                StartDefend();
            }
            else if (Input.GetKeyUp(defendKey))
            {
                StopDefend();
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
            
            // Handle movement based on state
            if (isRolling)
            {
                // Rolling movement (uses root motion from animation)
                // The animation will handle the movement
            }
            else if (isDashing)
            {
                // Dashing movement (uses root motion from animation)
                // The animation will handle the movement
            }
            else if (isAttacking)
            {
                // Limited movement during attack
                Vector3 attackMove = moveDirection * (currentMoveSpeed * 0.3f);
                characterController.Move(attackMove * Time.deltaTime);
            }
            else
            {
                // Normal movement
                Vector3 move = moveDirection * currentMoveSpeed;
                characterController.Move(move * Time.deltaTime);
            }
            
            // Apply gravity
            characterController.Move(velocity * Time.deltaTime);
            
            // Rotate towards movement direction
            if (moveDirection != Vector3.zero)
            {
                transform.forward = moveDirection;
            }
        }
        
        private void HandleAnimations()
        {
            // Update battle state based on movement
            bool isInBattle = moveDirection.magnitude > 0.1f || isAttacking || isDefending;
            // Note: isInBattle parameter not available in current controller
            
            // Update move direction for animations
            if (moveDirection.magnitude > 0.1f)
            {
                float direction = Vector3.Dot(transform.forward, moveDirection);
                // Note: moveDirection parameter not available in current controller
            }
        }
        
        private void StartRoll()
        {
            isRolling = true;
            // Note: rollDirection parameter not available in current controller
            animationController.TriggerRoll();
            
            // Reset roll after animation duration
            Invoke(nameof(StopRoll), 0.8f);
        }
        
        private void StopRoll()
        {
            isRolling = false;
            animationController.StopRoll();
        }
        
        private void StartDash()
        {
            isDashing = true;
            // Note: dashDirection parameter not available in current controller
            animationController.TriggerDash();
            
            // Reset dash after animation duration
            Invoke(nameof(StopDash), 0.5f);
        }
        
        private void StopDash()
        {
            isDashing = false;
            animationController.StopDash();
        }
        
        private void StartAttack()
        {
            isAttacking = true;
            // Random attack for variety
            int attackNumber = Random.Range(1, 6);
            animationController.TriggerAttack(attackNumber);
            
            // Reset attack after animation duration
            Invoke(nameof(StopAttack), 1.2f);
        }
        
        private void StopAttack()
        {
            isAttacking = false;
            animationController.StopAttack();
        }
        
        private void StartDefend()
        {
            isDefending = true;
            animationController.SetDefending(true);
        }
        
        private void StopDefend()
        {
            isDefending = false;
            animationController.SetDefending(false);
        }
        
        // Public methods for external control
        public void TakeDamage()
        {
            if (!isAttacking && !isRolling && !isDashing)
            {
                // Random damage type for variety
                int damageType = Random.Range(1, 3);
                animationController.TriggerTakeDamage(damageType);
            }
        }
        
        public void Die()
        {
            animationController.TriggerDie();
            // Disable movement
            enabled = false;
        }
        
        public void SetDizzy(bool dizzy)
        {
            animationController.SetDizzy(dizzy);
        }
        
        public void SetVictory(bool victory)
        {
            animationController.SetVictory(victory);
        }
        
        public void SetLevelUp(bool levelUp)
        {
            animationController.SetLevelUp(levelUp);
        }
        
        public void SetTaunting(bool taunting)
        {
            animationController.SetTaunting(taunting);
        }
        
        // Note: These methods are available in the updated PlayerAnimationController
        // You can use them for additional functionality
    }
}
