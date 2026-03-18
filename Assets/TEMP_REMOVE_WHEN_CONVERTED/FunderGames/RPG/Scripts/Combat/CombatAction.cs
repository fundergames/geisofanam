using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    public abstract class CombatAction : ScriptableObject
    {
        public string ActionName;
        public ActionType ActionType;
        public bool RequiresTarget;
        public TargetType TargetType;
        public ActionSequence actionSequence;  // The sequence of steps to execute

        // Execute the action by running the action sequence
        public virtual IEnumerator Execute(Combatant performer, Combatant target)
        {
            if (actionSequence != null)
            {
                yield return actionSequence.Execute(performer, target);
            }
        }
    }
    
    public enum ActionType
    {
        Attack,
        Magic,
        UseItem,
        Defend,
        Special
    }

    public enum TargetType
    {
        Enemy,
        Friend,
        All,
        Self,
        None
    }
}