using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace RogueDeal.Combat.States
{
    public class PlayerReturningState : CombatState
    {
        private bool returnComplete = false;
        private bool shouldTransitionToEnemyAttack = false;

        public PlayerReturningState(CombatFlowStateMachine context) : base(context) { }
        
        public void SetShouldTransitionToEnemyAttack(bool value)
        {
            shouldTransitionToEnemyAttack = value;
        }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering PlayerReturningState");
            returnComplete = false;
            shouldTransitionToEnemyAttack = context.CurrentEnemy != null && !context.CurrentEnemy.isDefeated;
            context.StartCoroutine(ReturnPlayer());
        }

        public override void OnTick(float dt)
        {
            if (returnComplete)
            {
                if (shouldTransitionToEnemyAttack)
                {
                    Debug.Log("[FSM] Player return complete, transitioning to EnemyAttackingState");
                    context.StateMachine.TryGo<EnemyAttackingState>();
                }
                else
                {
                    Debug.Log("[FSM] Player return complete, transitioning to ClearingHandState");
                    context.StateMachine.TryGo<ClearingHandState>();
                }
            }
        }

        private IEnumerator ReturnPlayer()
        {
            Debug.Log("[PlayerReturningState] Player returning to fight position");
            
            if (context.PlayerVisual == null)
            {
                Debug.LogWarning("[PlayerReturningState] PlayerVisual is null!");
                returnComplete = true;
                yield break;
            }

            Vector3 fightPos = context.IntroController != null ? context.IntroController.FightPosition : context.PlayerStartPosition;
            Debug.Log($"[PlayerReturningState] Fight position: {fightPos}");

            float distance = Vector3.Distance(context.PlayerVisual.transform.position, fightPos);
            float hopTime = distance / context.AttackReturnSpeed;
            
            Debug.Log($"[PlayerReturningState] Hopping back over {hopTime}s");

            Sequence hopSequence = DOTween.Sequence();
            hopSequence.Append(context.PlayerVisual.transform.DOMove(fightPos, hopTime).SetEase(Ease.InOutQuad));
            hopSequence.Join(context.PlayerVisual.transform.DOLocalMoveY(context.PlayerVisual.transform.position.y + 0.5f, hopTime * 0.5f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo));

            yield return hopSequence.WaitForCompletion();

            Debug.Log("[PlayerReturningState] Player return complete!");
            returnComplete = true;
        }
    }
}
