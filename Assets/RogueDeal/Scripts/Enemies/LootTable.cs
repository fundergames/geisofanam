using Funder.Core.Randoms;
using RogueDeal.Combat;
using RogueDeal.Items;
using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Enemies
{
    [CreateAssetMenu(fileName = "LootTable_", menuName = "Funder Games/Rogue Deal/Enemies/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Header("Drop Settings")]
        [Range(0f, 1f)]
        public float dropChance = 0.7f;
        public int minDrops = 0;
        public int maxDrops = 2;
        
        [Header("Loot Entries")]
        public List<LootEntry> entries = new List<LootEntry>();

        public List<BaseItem> RollLoot(IRandomHub randomHub)
        {
            if (randomHub == null)
            {
                Debug.LogError("RandomHub cannot be null. Provide an IRandomHub instance.");
                return new List<BaseItem>();
            }

            var loot = new List<BaseItem>();
            var stream = randomHub.GetStream($"Loot/{name}");

            if (stream.NextFloat() > dropChance)
                return loot;

            int dropCount = stream.NextInt(minDrops, maxDrops + 1);
            
            for (int i = 0; i < dropCount; i++)
            {
                var entry = SelectWeightedEntry(stream);
                if (entry != null && entry.item != null)
                {
                    loot.Add(entry.item);
                }
            }

            return loot;
        }

        private LootEntry SelectWeightedEntry(IRandomStream stream)
        {
            if (entries.Count == 0)
                return null;

            int totalWeight = 0;
            foreach (var entry in entries)
            {
                totalWeight += entry.weight;
            }

            int roll = stream.NextInt(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in entries)
            {
                currentWeight += entry.weight;
                if (roll < currentWeight)
                {
                    if (stream.NextFloat() <= entry.rarityChance)
                    {
                        return entry;
                    }
                }
            }

            return entries[0];
        }
    }

    [System.Serializable]
    public class LootEntry
    {
        public BaseItem item;
        public int weight = 10;
        
        [Header("Rarity")]
        public ItemRarity rarity = ItemRarity.Common;
        [Range(0f, 1f)]
        public float rarityChance = 1f;
        
        [Header("Quantity")]
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }
}
