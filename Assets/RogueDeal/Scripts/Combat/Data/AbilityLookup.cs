using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Maps action names or indices to CombatActions for real-time combat.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityLookup", menuName = "RogueDeal/Combat/Ability Lookup")]
    public class AbilityLookup : ScriptableObject
    {
        [Header("Actions")]
        [SerializeField] private CombatAction[] actions = new CombatAction[0];

        public CombatAction GetAction(int index)
        {
            if (actions == null || index < 0 || index >= actions.Length)
                return null;
            return actions[index];
        }

        public CombatAction GetActionByName(string actionName)
        {
            if (actions == null || string.IsNullOrEmpty(actionName))
                return null;
            foreach (var a in actions)
            {
                if (a != null && a.actionName == actionName)
                    return a;
            }
            return null;
        }

        public bool HasAction(int index)
        {
            return GetAction(index) != null;
        }

        public int ActionCount => actions != null ? actions.Length : 0;
    }
}
