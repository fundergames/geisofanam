using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "JumpOutOfCardStep", menuName = "FunderGames/ActionSteps/JumpOutOfCardStep")]
    public class JumpOutOfCardStep : ActionStep
    {
        [SerializeField] private float jumpPower = 3f; // Height of the jump out of card
        [SerializeField] private float duration = 0.8f; // Duration of the jump
        [SerializeField] private float cardExitOffset = 2f; // How far from the card to land
        [SerializeField] private bool maintainStencilMasking = true; // Whether to maintain stencil masking when outside card
        
        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Store the original parent and position
            Transform originalParent = performer.transform.parent;
            Vector3 originalLocalPosition = performer.transform.localPosition;
            
            // Get the CardCombatant component
            var cardCombatant = performer.GetComponent<CardCombatant>();
            if (cardCombatant == null)
            {
                Debug.LogWarning("JumpOutOfCardStep requires a CardCombatant component!");
                yield break;
            }
            
            // Calculate the exit position (slightly in front of the card)
            Vector3 cardPosition = originalParent.position;
            Vector3 directionToTarget = (target.transform.position - cardPosition).normalized;
            Vector3 exitPosition = cardPosition + directionToTarget * cardExitOffset;
            
            // Store the exit position for returning later
            cardCombatant.SetExitPosition(exitPosition);
            
            // Maintain stencil masking if enabled
            if (maintainStencilMasking)
            {
                int stencilID = cardCombatant.GetStencilID();
                cardCombatant.ApplyStencilMasking(stencilID);
            }
            
            // Temporarily unparent the character so it can move in world space
            performer.transform.SetParent(null);
            
            // Jump out of the card with a dramatic arc
            yield return performer.transform
                .DOJump(exitPosition, jumpPower, 1, duration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();
        }
    }
}
