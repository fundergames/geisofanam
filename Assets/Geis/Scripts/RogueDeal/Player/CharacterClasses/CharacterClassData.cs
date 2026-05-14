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

using UnityEngine;

namespace RogueDeal.Player
{
    [CreateAssetMenu(fileName = "New Character Class", menuName = "Funder Games/Geis/Rogue Deal/Character/Character Class")]
    public class CharacterClassData : ScriptableObject
    {
        [SerializeField] private string classDisplayName;
        [TextArea(3, 6)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        
        public string ClassDisplayName => classDisplayName;
        public string Description => description;
        public Sprite Icon => icon;
    }
}
