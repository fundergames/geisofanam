using Funder.Core.FSM;

namespace RogueDeal.Combat.States
{
    public abstract class CombatState : StateNode
    {
        protected readonly CombatFlowStateMachine context;

        protected CombatState(CombatFlowStateMachine context)
        {
            this.context = context;
        }
    }
}
