using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "FunderGames/Items/ItemData")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public ItemEffectType effectType;
        public int effectValue; // The amount to heal, restore mana, or buff
    }
    
    public enum ItemEffectType
    {
        Heal,
        ManaRestore,
        Buff
    }
}