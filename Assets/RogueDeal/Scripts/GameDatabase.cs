using RogueDeal.Combat;
using RogueDeal.Combat.StatusEffects;
using RogueDeal.Crafting;
using RogueDeal.Enemies;
using RogueDeal.Items;
using RogueDeal.Levels;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueDeal
{
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Funder Games/Rogue Deal/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        [Header("Classes")]
        public List<ClassDefinition> classes = new List<ClassDefinition>();
        
        [Header("Poker Hands")]
        public List<PokerHandDefinition> pokerHands = new List<PokerHandDefinition>();
        
        [Header("Status Effects")]
        public List<StatusEffectDefinition> statusEffects = new List<StatusEffectDefinition>();
        
        [Header("Items")]
        public List<EquipmentItem> equipment = new List<EquipmentItem>();
        public List<CraftingIngredient> ingredients = new List<CraftingIngredient>();
        public List<ConsumableItem> consumables = new List<ConsumableItem>();
        
        [Header("Crafting")]
        public List<CraftingRecipe> recipes = new List<CraftingRecipe>();
        
        [Header("Enemies")]
        public List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        public List<LootTable> lootTables = new List<LootTable>();
        
        [Header("Worlds & Levels")]
        public List<WorldDefinition> worlds = new List<WorldDefinition>();

        public ClassDefinition GetClass(CharacterClass classType)
        {
            return classes.FirstOrDefault(c => c.classType == classType);
        }

        public PokerHandDefinition GetPokerHand(PokerHandType handType)
        {
            return pokerHands.FirstOrDefault(h => h.handType == handType);
        }

        public StatusEffectDefinition GetStatusEffect(StatusEffectType effectType)
        {
            return statusEffects.FirstOrDefault(e => e.effectType == effectType);
        }

        public EnemyDefinition GetEnemy(string enemyId)
        {
            return enemies.FirstOrDefault(e => e.enemyId == enemyId);
        }

        public WorldDefinition GetWorld(int worldNumber)
        {
            return worlds.FirstOrDefault(w => w.worldNumber == worldNumber);
        }

        public LevelDefinition GetLevel(int worldNumber, int levelNumber)
        {
            var world = GetWorld(worldNumber);
            return world?.GetLevel(levelNumber);
        }

        public BaseItem GetItem(string itemId)
        {
            BaseItem item = equipment.FirstOrDefault(e => e.itemId == itemId);
            if (item != null) return item;

            item = ingredients.FirstOrDefault(i => i.itemId == itemId);
            if (item != null) return item;

            item = consumables.FirstOrDefault(c => c.itemId == itemId);
            return item;
        }

        public CraftingRecipe GetRecipe(string recipeId)
        {
            return recipes.FirstOrDefault(r => r.recipeId == recipeId);
        }
    }
}
