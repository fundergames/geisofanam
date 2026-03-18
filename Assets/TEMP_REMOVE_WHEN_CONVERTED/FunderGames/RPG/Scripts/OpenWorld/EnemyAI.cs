using UnityEngine;
using UnityEngine.AI;

namespace FunderGames.RPG.OpenWorld
{
    /// <summary>
    /// Enemy AI controller with patrol, chase, and attack behaviors
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float patrolRadius = 10f;
        [SerializeField] private float waitTime = 2f;
        
        [Header("Combat Settings")]
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private LayerMask playerLayerMask = 1;
        
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] patrolPoints;
        
        // Private variables
        private NavMeshAgent agent;
        private Animator animator;
        private EnemyHealth health;
        private Transform currentPatrolTarget;
        private Vector3 startPosition;
        
        // AI State
        private AIState currentState = AIState.Patrol;
        private float lastAttackTime;
        private float waitTimer;
        private bool isWaiting;
        
        // Animation parameters
        private readonly int speedHash = Animator.StringToHash("Speed");
        private readonly int isAttackingHash = Animator.StringToHash("IsAttacking");
        private readonly int isChasingHash = Animator.StringToHash("IsChasing");
        
        public enum AIState
        {
            Patrol,
            Chase,
            Attack,
            Return
        }
        
        private void Start()
        {
            InitializeComponents();
            SetupPatrol();
        }
        
        private void Update()
        {
            UpdateAI();
            UpdateAnimations();
        }
        
        /// <summary>
        /// Initialize required components
        /// </summary>
        private void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            health = GetComponent<EnemyHealth>();
            
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
                
            startPosition = transform.position;
        }
        
        /// <summary>
        /// Setup patrol system
        /// </summary>
        private void SetupPatrol()
        {
            if (patrolPoints.Length == 0)
            {
                // Generate random patrol points around start position
                patrolPoints = new Transform[3];
                for (int i = 0; i < 3; i++)
                {
                    GameObject point = new GameObject($"PatrolPoint_{i}");
                    point.transform.position = startPosition + Random.insideUnitSphere * patrolRadius;
                    point.transform.position = new Vector3(point.transform.position.x, startPosition.y, point.transform.position.z);
                    patrolPoints[i] = point.transform;
                }
            }
            
            SetNextPatrolTarget();
        }
        
        /// <summary>
        /// Main AI update loop
        /// </summary>
        private void UpdateAI()
        {
            if (health != null && health.IsDead)
            {
                currentState = AIState.Patrol;
                return;
            }
            
            // Check for player detection
            if (player != null && Vector3.Distance(transform.position, player.position) <= detectionRange)
            {
                if (Vector3.Distance(transform.position, player.position) <= attackRange)
                {
                    currentState = AIState.Attack;
                }
                else
                {
                    currentState = AIState.Chase;
                }
            }
            else if (currentState == AIState.Chase)
            {
                currentState = AIState.Return;
            }
            
            // Execute current state
            switch (currentState)
            {
                case AIState.Patrol:
                    ExecutePatrol();
                    break;
                case AIState.Chase:
                    ExecuteChase();
                    break;
                case AIState.Attack:
                    ExecuteAttack();
                    break;
                case AIState.Return:
                    ExecuteReturn();
                    break;
            }
        }
        
        /// <summary>
        /// Execute patrol behavior
        /// </summary>
        private void ExecutePatrol()
        {
            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    isWaiting = false;
                    SetNextPatrolTarget();
                }
                return;
            }
            
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                isWaiting = true;
                waitTimer = waitTime;
                return;
            }
        }
        
        /// <summary>
        /// Execute chase behavior
        /// </summary>
        private void ExecuteChase()
        {
            if (player != null)
            {
                agent.SetDestination(player.position);
                agent.speed = agent.speed * 1.5f; // Run faster when chasing
            }
        }
        
        /// <summary>
        /// Execute attack behavior
        /// </summary>
        private void ExecuteAttack()
        {
            if (player == null) return;
            
            // Face the player
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            
            // Attack if cooldown is ready
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }
        
        /// <summary>
        /// Execute return to patrol behavior
        /// </summary>
        private void ExecuteReturn()
        {
            if (Vector3.Distance(transform.position, startPosition) <= 1f)
            {
                currentState = AIState.Patrol;
                agent.speed = agent.speed / 1.5f; // Reset to normal speed
                SetNextPatrolTarget();
            }
            else
            {
                agent.SetDestination(startPosition);
            }
        }
        
        /// <summary>
        /// Set next patrol target
        /// </summary>
        private void SetNextPatrolTarget()
        {
            if (patrolPoints.Length == 0) return;
            
            if (currentPatrolTarget == null)
            {
                currentPatrolTarget = patrolPoints[0];
            }
            else
            {
                // Find next patrol point
                int currentIndex = System.Array.IndexOf(patrolPoints, currentPatrolTarget);
                int nextIndex = (currentIndex + 1) % patrolPoints.Length;
                currentPatrolTarget = patrolPoints[nextIndex];
            }
            
            agent.SetDestination(currentPatrolTarget.position);
        }
        
        /// <summary>
        /// Perform attack on player
        /// </summary>
        private void PerformAttack()
        {
            if (player == null) return;
            
            // Check if player is still in range
            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                var playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                    Debug.Log($"{name} attacked player for {attackDamage} damage!");
                }
            }
        }
        
        /// <summary>
        /// Update animations based on current state
        /// </summary>
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // Speed animation
            animator.SetFloat(speedHash, agent.velocity.magnitude);
            
            // State animations
            animator.SetBool(isAttackingHash, currentState == AIState.Attack);
            animator.SetBool(isChasingHash, currentState == AIState.Chase);
        }
        
        /// <summary>
        /// Draw AI ranges in editor
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Patrol radius
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(startPosition, patrolRadius);
            
            // Patrol points
            if (patrolPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in patrolPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.5f);
                    }
                }
            }
        }
        
        /// <summary>
        /// Get current AI state
        /// </summary>
        public AIState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// Force AI to chase player (for testing)
        /// </summary>
        public void ForceChase()
        {
            currentState = AIState.Chase;
        }
        
        /// <summary>
        /// Reset AI to patrol state
        /// </summary>
        public void ResetToPatrol()
        {
            currentState = AIState.Patrol;
            agent.SetDestination(startPosition);
        }
    }
}
