namespace RogueDeal.Combat.States
{
    public class ReadyToDealAgainState : CombatState
    {
        public ReadyToDealAgainState(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            UnityEngine.Debug.Log("[FSM] Entering ReadyToDealAgainState - dealing new hand");
            context.CombatManager.DealNewHand();
        }
    }
}
