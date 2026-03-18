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
