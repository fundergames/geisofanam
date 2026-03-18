using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "ActionSequence", menuName = "FunderGames/ActionSequence")]
    public class ActionSequence : ScriptableObject
    {
        public List<ActionStep> steps = new(); // The steps in this sequence

        // Execute all steps in sequence
        public virtual IEnumerator Execute(Combatant performer, Combatant target)
        {
            foreach (var step in steps)
            {
                yield return step.Execute(performer, target); // Execute each step sequentially
            }
        }
    }
}