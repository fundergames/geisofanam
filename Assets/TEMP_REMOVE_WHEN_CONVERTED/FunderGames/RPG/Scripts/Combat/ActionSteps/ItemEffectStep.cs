using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "ItemEffectStep", menuName = "FunderGames/ActionSteps/ItemEffectStep")]
    public class ItemEffectStep : ActionStep
    {
        public ItemData itemData;

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Apply the item's effects (heal, buff, etc.)
            switch (itemData.effectType)
            {
                case ItemEffectType.Heal:
                    target.Heal(itemData.effectValue);
                    break;
                case ItemEffectType.ManaRestore:
                    target.AdjustMana(itemData.effectValue);
                    break;
                case ItemEffectType.Buff:
                    // Apply buff logic
                    break;
            }

            yield return null;
        }
    }
}