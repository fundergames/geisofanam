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
