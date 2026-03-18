using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "MagicDamageStep", menuName = "FunderGames/ActionSteps/MagicDamageStep")]
    public class MagicDamageStep : ActionStep
    {
        public int magicDamage;

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            target.TakeDamage(magicDamage); // Apply magic damage to the target
            yield return null;
        }
    }
}