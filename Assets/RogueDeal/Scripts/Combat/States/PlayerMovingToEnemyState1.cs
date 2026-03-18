using System.Collections;
using UnityEngine;

namespace RogueDeal.Combat.States
{
    public class PlayerMovingToEnemyState : CombatState
    {
        private bool movementComplete = false;

        public PlayerMovingToEnemyState(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering PlayerMovingToEnemyState");
            movementComplete = false;
            context.StartCoroutine(MoveToNextEnemy());
        }

        public override void OnTick(float dt)
        {
            if (movementComplete)
            {
                Debug.Log("[FSM] Movement complete, transitioning to DealingCardsState");
                context.StateMachine.TryGo<DealingCardsState>();
            }
        }

        private IEnumerator MoveToNextEnemy()
        {
            if (context.IntroController != null)
            {
                yield return context.IntroController.RunToNextEnemy();
            }

            context.RefreshEnemyVisual();

            if (context.CombatManager != null)
            {
                Debug.Log("[PlayerMovingToEnemyState] Requesting new hand from CombatManager...");
                context.CombatManager.DealNewHand();
            }

            movementComplete = true;
        }
    }
}
