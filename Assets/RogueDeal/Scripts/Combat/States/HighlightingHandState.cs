using System.Collections;
using UnityEngine;

namespace RogueDeal.Combat.States
{
    public class HighlightingHandState : CombatState
    {
        private bool highlightComplete = false;

        public HighlightingHandState(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            Debug.Log("[FSM] Entering HighlightingHandState");
            highlightComplete = false;
            context.StartCoroutine(HighlightAndWait());
        }

        public override void OnTick(float dt)
        {
            if (highlightComplete)
            {
                Debug.Log("[FSM] Highlight complete, transitioning to PlayerAttackingState");
                context.StateMachine.TryGo<PlayerAttackingState>();
            }
        }

        private IEnumerator HighlightAndWait()
        {
            Debug.Log("[HighlightingHandState] Waiting for animations to complete...");
            yield return new WaitUntil(() => !context.CardHandUI.IsAnimating);

            Debug.Log("[HighlightingHandState] Evaluating current hand...");
            context.CombatManager.EvaluateCurrentHand();

            if (context.LastHandType.HasValue)
            {
                Debug.Log($"[HighlightingHandState] Highlighting {context.LastHandType.Value} cards...");
                yield return context.CardHandUI.HighlightWinningCards(
                    context.LastHandType.Value, 
                    context.CombatManager.CurrentHand
                );
            }

            Debug.Log($"[HighlightingHandState] Waiting {context.HighlightDuration} seconds...");
            yield return new WaitForSeconds(context.HighlightDuration);

            Debug.Log("[HighlightingHandState] Highlight sequence complete!");
            highlightComplete = true;
        }
    }
}
