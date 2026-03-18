using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "CardAttackStep", menuName = "FunderGames/ActionSteps/CardAttackStep")]
    public class CardAttackStep : ActionStep
    {
        [Header("Attack Settings")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private string attackAnimationTrigger = "Attack01_NoWeapon";
        [SerializeField] private float attackDuration = 0.8f; // How long the attack takes
        [SerializeField] private float damageDelay = 0.3f; // When to apply damage during the attack
        
        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Play the attack animation
            performer.PlayAnimation(attackAnimationTrigger);
            
            // Wait for the damage timing
            yield return new WaitForSeconds(damageDelay);
            
            // Apply damage to the target
            target.TakeDamage((int)attackDamage);
            
            // Wait for the attack animation to complete
            yield return new WaitForSeconds(attackDuration - damageDelay);
        }
    }
}
