using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using RogueDeal.UI;
using RogueDeal.Combat.Cards;

namespace RogueDeal.Combat.States
{
    public class DiscardingCardsState : CombatState
    {
        private bool animationComplete = false;

        public DiscardingCardsState(CombatFlowStateMachine context) : base(context) { }

        public override void OnEnter()
        {
            animationComplete = false;
            context.CardHandUI.SetCardsInteractable(false);
            context.StartCoroutine(DiscardAndReplaceCards());
        }

        public override void OnTick(float dt)
        {
            if (animationComplete && !context.CardHandUI.IsAnimating)
            {
                context.StateMachine.TryGo<HighlightingHandState>();
            }
        }

        private IEnumerator DiscardAndReplaceCards()
        {
            var heldFlags = context.CardHandUI.GetHeldCardFlags();
            var activeCards = context.CardHandUI.GetActiveCards();

            foreach (var card in activeCards.Where(c => c.IsHeld))
            {
                card.SetHeld(false);
            }

            List<int> unheldIndices = new List<int>();
            for (int i = 0; i < heldFlags.Count; i++)
            {
                if (!heldFlags[i])
                    unheldIndices.Add(i);
            }

            if (unheldIndices.Count > 0)
            {
                context.RaiseDrawCardsRequest(heldFlags);
                
                yield return new WaitForSeconds(0.1f);
                
                var updatedHand = context.CombatManager.CurrentHand;
                List<Card> replacementCards = new List<Card>();
                foreach (int index in unheldIndices)
                {
                    if (index < updatedHand.Count)
                        replacementCards.Add(updatedHand[index]);
                }
                
                yield return context.CardHandUI.ReplaceCards(unheldIndices, replacementCards);
            }

            animationComplete = true;
        }

        private IEnumerator DiscardCards(List<int> indices)
        {
            var cards = context.CardHandUI.GetActiveCards();
            Sequence discardSequence = DOTween.Sequence();
            Vector3 discardPos = context.CardHandUI.LayoutConfig.discardPosition;

            for (int i = 0; i < indices.Count; i++)
            {
                int index = indices[i];
                if (index < cards.Count)
                {
                    discardSequence.Insert(
                        i * context.DiscardCardDelay,
                        cards[index].AnimateDiscard(discardPos, 0.4f)
                    );
                }
            }

            yield return discardSequence.WaitForCompletion();
        }
    }
}
