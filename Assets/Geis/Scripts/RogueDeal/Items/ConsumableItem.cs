using RogueDeal.Combat.StatusEffects;
using UnityEngine;

namespace RogueDeal.Items
{
    [CreateAssetMenu(fileName = "Consumable_", menuName = "Funder Games/Rogue Deal/Items/Consumable")]
    public class ConsumableItem : BaseItem
    {
        [Header("Consumable Properties")]
        public int healthRestore = 0;
        public int energyRestore = 0;
        
        [Header("Status Effects")]
        public StatusEffectDefinition[] effectsToApply;
        
        [Header("Buffs")]
        public float duration = 0f;
        public float damageBoostPercent = 0f;
        public float defenseBoostPercent = 0f;

        public ConsumableItem()
        {
            maxStackSize = 99;
        }
    }
}
