using UnityEngine;

namespace RogueDeal.Combat.States
{
    public class DealingCardsState : CombatState
    {
        public DealingCardsState(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering DealingCardsState");
            context.SetButtonEnabled(false);
        }

        public override void OnTick(float dt)
        {
            if (context.CardHandUI == null)
            {
                Debug.LogWarning("[DealingCardsState] CardHandUI is null!");
                return;
            }

            if (!context.CardHandUI.IsAnimating)
            {
                Debug.Log("[FSM] Dealing complete, transitioning to WaitingForPlayerState");
                context.StateMachine.TryGo<WaitingForPlayerState>();
            }
        }

        public override void OnExit()
        {
            Debug.Log("[FSM] Exiting DealingCardsState");
        }
    }
}
