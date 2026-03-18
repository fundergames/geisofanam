using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace RogueDeal.Combat.States
{
    public class EnemyAttackingState : CombatState
    {
        private bool attackComplete = false;
        private bool dodged = false;
        private int damageDealt = 0;

        public EnemyAttackingState(CombatFlowStateMachine context) : base(context) { }

        public void SetAttackData(bool wasDodged, int damage)
        {
            dodged = wasDodged;
            damageDealt = damage;
        }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering EnemyAttackingState");
            attackComplete = false;
            context.StartCoroutine(PerformAttack());
        }

        public override void OnTick(float dt)
        {
            if (attackComplete)
            {
                Debug.Log("[FSM] Enemy attack complete, transitioning to ClearingHandState");
                context.StateMachine.TryGo<ClearingHandState>();
            }
        }

        private IEnumerator PerformAttack()
        {
            Debug.Log($"[EnemyAttackingState] Starting attack - Dodge: {dodged}, Damage: {damageDealt}");
            Debug.Log($"[EnemyAttackingState] Waiting {context.DelayBeforeEnemyAttack}s before enemy attack");
            yield return new WaitForSeconds(context.DelayBeforeEnemyAttack);

            if (context.CurrentEnemyVisual == null || context.PlayerVisual == null)
            {
                Debug.LogWarning("[EnemyAttackingState] Missing visuals - CurrentEnemy or Player is null!");
                attackComplete = true;
                yield break;
            }

            Debug.Log("[EnemyAttackingState] Enemy jumping to attack position");
            Vector3 enemyStartPosition = context.CurrentEnemyVisual.EnemyStartPosition;
            Vector3 attackPosition = context.PlayerVisual.transform.position + Vector3.right * 2f;

            if (context.CurrentEnemyVisual.Animator != null)
                context.CurrentEnemyVisual.Animator.SetTrigger("Move");

            float distance = Vector3.Distance(context.CurrentEnemyVisual.transform.position, attackPosition);
            float moveTime = distance / 5f;

            yield return context.CurrentEnemyVisual.transform.DOMove(attackPosition, moveTime).SetEase(Ease.Linear).WaitForCompletion();

            Debug.Log("[EnemyAttackingState] Enemy reached attack position, performing attack");
            if (context.CurrentEnemyVisual.Animator != null)
            {
                context.CurrentEnemyVisual.Animator.SetTrigger("Attack_1");
            }

            context.CombatManager.PerformEnemyAttack();

            yield return new WaitForSeconds(0.2f);

            if (dodged)
            {
                Debug.Log("[EnemyAttackingState] Player dodged!");
            }
            else
            {
                Debug.Log($"[EnemyAttackingState] Player took {damageDealt} damage");
                if (context.PlayerVisual != null)
                {
                    if (context.PlayerVisual.Animator != null)
                        context.PlayerVisual.Animator.SetTrigger("TakeDamage");

                    yield return context.PlayerVisual.AnimateDamage(damageDealt).WaitForCompletion();
                }
            }

            Debug.Log($"[EnemyAttackingState] Waiting {context.DelayAfterEnemyAttack}s after attack");
            yield return new WaitForSeconds(context.DelayAfterEnemyAttack);

            Debug.Log("[EnemyAttackingState] Enemy jumping back to start position");
            if (context.CurrentEnemyVisual.Animator != null)
                context.CurrentEnemyVisual.Animator.SetTrigger("Move");

            float returnDistance = Vector3.Distance(context.CurrentEnemyVisual.transform.position, enemyStartPosition);
            float returnTime = returnDistance / 5f;

            yield return context.CurrentEnemyVisual.transform.DOMove(enemyStartPosition, returnTime).SetEase(Ease.Linear).WaitForCompletion();

            Debug.Log("[EnemyAttackingState] Attack complete!");
            attackComplete = true;
        }
    }
}
