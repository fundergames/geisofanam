using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace RogueDeal.Combat.States
{
    public class ClearingHandState : CombatState
    {
        private bool clearComplete = false;

        public ClearingHandState(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            clearComplete = false;
            context.StartCoroutine(ClearHand());
        }

        public override void OnTick(float dt)
        {
            if (clearComplete)
            {
                Debug.Log("[FSM] Hand cleared, transitioning to ReadyToDealAgainState");
                context.StateMachine.TryGo<ReadyToDealAgainState>();
            }
        }

        private IEnumerator ClearHand()
        {
            var cards = context.CardHandUI.GetActiveCards();
            Sequence clearSequence = DOTween.Sequence();
            Vector3 discardPos = context.CardHandUI.LayoutConfig.discardPosition;

            for (int i = 0; i < cards.Count; i++)
            {
                clearSequence.Insert(
                    i * context.DiscardCardDelay,
                    cards[i].AnimateDiscard(discardPos, 0.4f)
                );
            }

            yield return clearSequence.WaitForCompletion();

            context.CardHandUI.ClearHand();

            clearComplete = true;
        }
    }
}
