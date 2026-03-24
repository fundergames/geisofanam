using RogueDeal.Items;
using System;
using UnityEngine;

namespace RogueDeal.Crafting
{
    [CreateAssetMenu(fileName = "Recipe_", menuName = "Funder Games/Rogue Deal/Crafting/Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("Recipe Info")]
        public string recipeId;
        public string recipeName;
        [TextArea(2, 3)]
        public string description;
        
        [Header("Requirements")]
        public RecipeSlot slotA;
        public RecipeSlot slotB;
        public RecipeSlot slotC;
        
        [Header("Output")]
        public BaseItem outputItem;
        public int outputQuantity = 1;
        
        [Header("Quality Modifiers")]
        public bool allowQualityRoll = true;
        public float rarityUpgradeChance = 0.1f;

        public bool MatchesIngredients(CraftingIngredient ingredientA, CraftingIngredient ingredientB, CraftingIngredient ingredientC)
        {
            bool matchA = slotA.Matches(ingredientA);
            bool matchB = slotB.Matches(ingredientB);
            bool matchC = slotC.Matches(ingredientC);
            
            return matchA && matchB && matchC;
        }
    }

    [Serializable]
    public class RecipeSlot
    {
        public IngredientCategory requiredCategory;
        public string specificType;
        public bool allowAnyType = true;

        public bool Matches(CraftingIngredient ingredient)
        {
            if (ingredient == null)
                return false;

            if (ingredient.category != requiredCategory)
                return false;

            if (!allowAnyType && !string.IsNullOrEmpty(specificType))
            {
                return ingredient.ingredientType == specificType;
            }

            return true;
        }
    }
}
