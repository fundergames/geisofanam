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

using RogueDeal.Combat;
using UnityEngine;

namespace RogueDeal.Items
{
    public abstract class BaseItem : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        
        [Header("Properties")]
        public ItemRarity rarity = ItemRarity.Common;
        public int goldValue = 10;
        public int maxStackSize = 1;
        public bool isTradeable = true;
    }
}
