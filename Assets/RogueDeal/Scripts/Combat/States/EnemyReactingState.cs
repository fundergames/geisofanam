using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace RogueDeal.Combat.States
{
    public class EnemyReactingState : CombatState
    {
        private int damageDealt;
        private bool isDefeated;
        private bool isCritical;
        private bool reactionComplete = false;

        public EnemyReactingState(CombatFlowStateMachine context) : base(context) { }

        public void SetReactionData(int damage, bool defeated, bool isCrit = false)
        {
            damageDealt = damage;
            isDefeated = defeated;
            isCritical = isCrit;
        }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering EnemyReactingState");
            reactionComplete = false;
            context.StartCoroutine(DetermineNextState());
        }

        public override void OnTick(float dt)
        {
            if (reactionComplete)
            {
                if (isDefeated)
                {
                    Debug.Log("[FSM] Enemy defeated, progressing to next enemy...");
                    context.CombatManager.ProgressToNextEnemy();
                    Debug.Log("[FSM] Transitioning to PlayerMovingToEnemyState");
                    context.StateMachine.TryGo<PlayerMovingToEnemyState>();
                }
                else
                {
                    Debug.Log("[FSM] Enemy survived, transitioning to PlayerReturningState");
                    context.StateMachine.TryGo<PlayerReturningState>();
                }
            }
        }

        private IEnumerator DetermineNextState()
        {
            Debug.Log($"[EnemyReactingState] Determining next state - Enemy defeated: {isDefeated}");
            
            if (context.CurrentEnemyVisual == null && !isDefeated)
            {
                Debug.LogWarning("[EnemyReactingState] CurrentEnemyVisual is null but enemy not defeated!");
            }

            yield return new WaitForSeconds(0.1f);

            reactionComplete = true;
            Debug.Log("[EnemyReactingState] Reaction check complete!");
        }
    }
}
