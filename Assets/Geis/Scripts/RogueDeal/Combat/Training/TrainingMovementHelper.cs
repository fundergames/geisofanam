using UnityEngine;
using System.Collections;

namespace RogueDeal.Combat.Training
{
    public class TrainingMovementHelper : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private bool enableMovement = true;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private bool returnToStart = true;
        
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform target;
        
        private Vector3 playerStartPosition;
        private Quaternion playerStartRotation;
        private bool isMoving = false;
        
        private void Start()
        {
            if (player != null)
            {
                playerStartPosition = player.position;
                playerStartRotation = player.rotation;
            }
        }
        
        private void OnEnable()
        {
            CombatEvents.OnAttackStarted += OnAttackStarted;
            CombatEvents.OnAttackCompleted += OnAttackCompleted;
        }
        
        private void OnDisable()
        {
            CombatEvents.OnAttackStarted -= OnAttackStarted;
            CombatEvents.OnAttackCompleted -= OnAttackCompleted;
        }
        
        private void OnAttackStarted(CombatEventData eventData)
        {
            if (!enableMovement || player == null || target == null)
                return;
                
            if (eventData.source.transform == player)
            {
                StartCoroutine(MoveToAttack());
            }
        }
        
        private void OnAttackCompleted(CombatEventData eventData)
        {
            if (!enableMovement || !returnToStart || player == null)
                return;
                
            if (eventData.source.transform == player)
            {
                StartCoroutine(ReturnToStart());
            }
        }
        
        private IEnumerator MoveToAttack()
        {
            if (isMoving)
                yield break;
                
            isMoving = true;
            
            Vector3 targetPosition = target.position;
            Vector3 direction = (targetPosition - player.position).normalized;
            Vector3 attackPosition = targetPosition - direction * attackRange;
            
            player.rotation = Quaternion.LookRotation(direction);
            
            while (Vector3.Distance(player.position, attackPosition) > 0.1f)
            {
                player.position = Vector3.MoveTowards(player.position, attackPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            
            isMoving = false;
        }
        
        private IEnumerator ReturnToStart()
        {
            yield return new WaitForSeconds(0.5f);
            
            if (isMoving)
                yield break;
                
            isMoving = true;
            
            while (Vector3.Distance(player.position, playerStartPosition) > 0.1f)
            {
                player.position = Vector3.MoveTowards(player.position, playerStartPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            
            player.rotation = Quaternion.Slerp(player.rotation, playerStartRotation, Time.deltaTime * 5f);
            
            isMoving = false;
        }
        
        [ContextMenu("Auto-Find References")]
        private void AutoFindReferences()
        {
            CombatEntity[] entities = FindObjectsByType<CombatEntity>(FindObjectsSortMode.None);
            
            foreach (var entity in entities)
            {
                if (entity.GetComponent<TrainingDummy>() != null)
                {
                    target = entity.transform;
                }
                else
                {
                    player = entity.transform;
                }
            }
            
            if (player != null)
            {
                playerStartPosition = player.position;
                playerStartRotation = player.rotation;
            }
            
            Debug.Log($"[TrainingMovementHelper] Found - Player: {(player != null ? player.name : "None")}, Target: {(target != null ? target.name : "None")}");
        }
    }
}
