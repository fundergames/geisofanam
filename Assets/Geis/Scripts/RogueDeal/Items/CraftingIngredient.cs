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

namespace RogueDeal.Items
{
    [CreateAssetMenu(fileName = "Ingredient_", menuName = "Funder Games/Rogue Deal/Items/Crafting Ingredient")]
    public class CraftingIngredient : BaseItem
    {
        [Header("Crafting Properties")]
        public IngredientCategory category;
        public int quality = 1;
        
        [Header("Ingredient Data")]
        public string ingredientType;
        public string subType;

        public CraftingIngredient()
        {
            maxStackSize = 99;
        }
    }

    public enum IngredientCategory
    {
        WeaponMold,
        Liquid,
        BuffEssence,
        Herb,
        CoreMaterial,
        Catalyst
    }
}
