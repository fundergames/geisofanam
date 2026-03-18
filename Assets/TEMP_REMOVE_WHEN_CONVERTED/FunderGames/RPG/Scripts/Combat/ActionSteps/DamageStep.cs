using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "DamageStep", menuName = "FunderGames/ActionSteps/DamageStep")]
    public class DamageStep : ActionStep
    {
        public int damageAmount; // Amount of damage to deal

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Apply damage to the target
            target.TakeDamage(damageAmount);
            yield return null; // No delay needed, can proceed immediately
        }
    }
}