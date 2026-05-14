/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Maps action names or indices to CombatActions for real-time combat.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilityLookup", menuName = "Funder Games/Geis/Rogue Deal/Combat/Ability Lookup")]
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
