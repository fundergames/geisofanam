using UnityEngine;

namespace RogueDeal.Combat.States
{
    public class WaitingForPlayerState : CombatState
    {
        public WaitingForPlayerState(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering WaitingForPlayerState - player can now interact");
            context.SetButtonText("Draw");
            context.SetButtonEnabled(true);
            
            if (context.CardHandUI != null)
                context.CardHandUI.SetCardsInteractable(true);
        }

        public override void OnExit()
        {
            Debug.Log("[FSM] Exiting WaitingForPlayerState");
            context.SetButtonEnabled(false);
            
            if (context.CardHandUI != null)
                context.CardHandUI.SetCardsInteractable(false);
        }
    }
}
