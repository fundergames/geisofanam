using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "ManaDeductionStep", menuName = "FunderGames/ActionSteps/ManaDeductionStep")]
    public class ManaDeductionStep : ActionStep
    {
        public int manaCost;

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Deduct mana from the performer
            performer.AdjustMana(-manaCost);
            yield return null;
        }
    }
}