using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "CardAttackAction", menuName = "FunderGames/CombatActions/CardAttackAction")]
    public class CardAttackAction : CombatAction
    {
        [Header("Card Attack Settings")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private string attackAnimationTrigger = "Attack01_NoWeapon";
        
        public CardAttackAction()
        {
            ActionName = "Card Attack";
            ActionType = ActionType.Attack;
            RequiresTarget = true;
            TargetType = TargetType.Enemy;
        }

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Check if the performer has the CardCombatant component
            var cardCombatant = performer.GetComponent<CardCombatant>();
            if (cardCombatant == null)
            {
                Debug.LogWarning("CardAttackAction requires a CardCombatant component!");
                yield break;
            }
            
            // Use the action sequence to define the order of events
            if (actionSequence != null)
            {
                yield return actionSequence.Execute(performer, target);
            }
            else
            {
                // Fallback sequence if no action sequence is defined
                yield return ExecuteDefaultCardAttack(performer, target);
            }
        }
        
        private IEnumerator ExecuteDefaultCardAttack(Combatant performer, Combatant target)
        {
            // 1. Jump out of the card
            var jumpOutStep = ScriptableObject.CreateInstance<JumpOutOfCardStep>();
            yield return jumpOutStep.Execute(performer, target);
            
            // 2. Move to target
            var moveStep = ScriptableObject.CreateInstance<CardWorldMovementStep>();
            yield return moveStep.Execute(performer, target);
            
            // 3. Play attack animation
            performer.PlayAnimation(attackAnimationTrigger);
            yield return new WaitForSeconds(0.5f); // Wait for animation
            
            // 4. Deal damage
            target.TakeDamage((int)attackDamage);
            
            // 5. Jump back into the card
            var jumpBackStep = ScriptableObject.CreateInstance<JumpBackIntoCardStep>();
            yield return jumpBackStep.Execute(performer, target);
        }
    }
}
