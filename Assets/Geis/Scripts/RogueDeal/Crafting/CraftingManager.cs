using Funder.Core.Events;
using Funder.Core.Randoms;
using RogueDeal.Combat;
using RogueDeal.Events;
using RogueDeal.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueDeal.Crafting
{
    public class CraftingManager
    {
        private readonly List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
        private readonly IRandomHub _randomHub;
        private CraftingIngredient slotA;
        private CraftingIngredient slotB;
        private CraftingIngredient slotC;

        public CraftingIngredient SlotA => slotA;
        public CraftingIngredient SlotB => slotB;
        public CraftingIngredient SlotC => slotC;
        public IReadOnlyList<CraftingRecipe> AvailableRecipes => availableRecipes;

        public CraftingManager(IRandomHub randomHub)
        {
            _randomHub = randomHub ?? throw new System.ArgumentNullException(nameof(randomHub));
        }

        public void LoadRecipes(CraftingRecipe[] recipes)
        {
            availableRecipes.Clear();
            availableRecipes.AddRange(recipes);
        }

        public bool SetSlot(int slotIndex, CraftingIngredient ingredient)
        {
            switch (slotIndex)
            {
                case 0:
                    slotA = ingredient;
                    return true;
                case 1:
                    slotB = ingredient;
                    return true;
                case 2:
                    slotC = ingredient;
                    return true;
                default:
                    return false;
            }
        }

        public void ClearSlot(int slotIndex)
        {
            SetSlot(slotIndex, null);
        }

        public void ClearAllSlots()
        {
            slotA = null;
            slotB = null;
            slotC = null;
        }

        public bool CanCraft()
        {
            return slotA != null && slotB != null && slotC != null;
        }

        public CraftingRecipe FindMatchingRecipe()
        {
            if (!CanCraft())
                return null;

            return availableRecipes.FirstOrDefault(r => r.MatchesIngredients(slotA, slotB, slotC));
        }

        public CraftingResult Craft()
        {
            var recipe = FindMatchingRecipe();
            if (recipe == null)
            {
                return new CraftingResult
                {
                    success = false,
                    errorMessage = "No valid recipe found for these ingredients."
                };
            }

            var result = new CraftingResult
            {
                success = true,
                outputItem = recipe.outputItem,
                quantity = recipe.outputQuantity
            };

            if (recipe.allowQualityRoll && recipe.outputItem is EquipmentItem equipment)
            {
                var stream = _randomHub.GetStream("Crafting/Quality");
                float roll = stream.NextFloat01();
                if (roll <= recipe.rarityUpgradeChance)
                {
                    result.rarityUpgraded = true;
                }

                int qualityBonus = (slotA?.quality ?? 0) + (slotB?.quality ?? 0) + (slotC?.quality ?? 0);
                result.qualityModifier = qualityBonus / 3f;
            }

            EventBus<ItemCraftedEvent>.Raise(new ItemCraftedEvent
            {
                recipe = recipe,
                result = result,
                ingredientA = slotA,
                ingredientB = slotB,
                ingredientC = slotC
            });

            ClearAllSlots();

            return result;
        }
    }

    public class CraftingResult
    {
        public bool success;
        public string errorMessage;
        public BaseItem outputItem;
        public int quantity;
        public bool rarityUpgraded;
        public float qualityModifier;
        public ItemRarity finalRarity;
    }
}
