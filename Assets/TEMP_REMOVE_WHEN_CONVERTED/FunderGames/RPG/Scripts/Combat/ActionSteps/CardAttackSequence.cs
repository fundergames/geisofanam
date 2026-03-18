using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "CardAttackSequence", menuName = "FunderGames/ActionSequences/CardAttackSequence")]
    public class CardAttackSequence : ActionSequence
    {
        [Header("Card Attack Sequence Settings")]
#pragma warning disable CS0414
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private string attackAnimationTrigger = "Attack01_NoWeapon";
#pragma warning restore CS0414
        
        private void OnEnable()
        {
            // Create the default card attack sequence if no steps are defined
            if (steps.Count == 0)
            {
                CreateDefaultCardAttackSequence();
            }
        }
        
        private void CreateDefaultCardAttackSequence()
        {
            // Clear existing steps
            steps.Clear();
            
            // 1. Jump out of the card
            var jumpOutStep = ScriptableObject.CreateInstance<JumpOutOfCardStep>();
            steps.Add(jumpOutStep);
            
            // 2. Move to target in world space
            var moveStep = ScriptableObject.CreateInstance<CardWorldMovementStep>();
            steps.Add(moveStep);
            
            // 3. Attack step (you can create a custom attack step or use existing ones)
            var attackStep = ScriptableObject.CreateInstance<CardAttackStep>();
            steps.Add(attackStep);
            
            // 4. Jump back into the card
            var jumpBackStep = ScriptableObject.CreateInstance<JumpBackIntoCardStep>();
            steps.Add(jumpBackStep);
        }
        
        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Verify the performer has the required component
            var cardCombatant = performer.GetComponent<CardCombatant>();
            if (cardCombatant == null)
            {
                Debug.LogWarning("CardAttackSequence requires a CardCombatant component!");
                yield break;
            }
            
            // Execute all steps in sequence
            foreach (var step in steps)
            {
                yield return step.Execute(performer, target);
            }
        }
    }
}
