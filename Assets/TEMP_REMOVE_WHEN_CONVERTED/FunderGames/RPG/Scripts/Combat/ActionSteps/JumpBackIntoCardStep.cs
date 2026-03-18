using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "JumpBackIntoCardStep", menuName = "FunderGames/ActionSteps/JumpBackIntoCardStep")]
    public class JumpBackIntoCardStep : ActionStep
    {
        [SerializeField] private float jumpPower = 3f; // Height of the jump back into card
        [SerializeField] private float duration = 0.8f; // Duration of the jump
        [SerializeField] private bool maintainStencilMasking = true; // Whether to maintain stencil masking when outside card
        
        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            var cardCombatant = performer.GetComponent<CardCombatant>();
            if (cardCombatant == null)
            {
                Debug.LogWarning("JumpBackIntoCardStep requires a CardCombatant component!");
                yield break;
            }
            
            // Get the card transform and original local position
            Transform cardTransform = cardCombatant.GetCardTransform();
            Vector3 originalLocalPosition = cardCombatant.GetOriginalLocalPosition();
            
            if (cardTransform == null)
            {
                Debug.LogWarning("Card transform not found!");
                yield break;
            }
            
            // Jump back to the card with a dramatic arc
            Vector3 cardWorldPosition = cardTransform.position;
            yield return performer.transform
                .DOJump(cardWorldPosition, jumpPower, 1, duration)
                .SetEase(Ease.InQuad)
                .WaitForCompletion();
            
            // Reparent the character back to the card
            performer.transform.SetParent(cardTransform);
            performer.transform.localPosition = originalLocalPosition;
            performer.transform.localRotation = Quaternion.identity;
            
            // Restore stencil masking to the card's stencil ID
            if (maintainStencilMasking)
            {
                int stencilID = cardCombatant.GetStencilID();
                cardCombatant.ApplyStencilMasking(stencilID);
            }
        }
    }
}
