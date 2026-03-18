namespace Funder.Core.FSM
{
    public abstract class StateNode
    {
        public virtual void OnEnter()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void OnTick(float dt)
        {
        }
    }
}
