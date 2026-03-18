using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "UseItemAction", menuName = "FunderGames/CombatActions/UseItemAction")]
    public class UseItemAction : CombatAction
    {
        public ItemData selectedItem; // The item to be used (item data should define effects)

        public UseItemAction()
        {
            ActionName = "Use Item";
            ActionType = ActionType.UseItem;
            RequiresTarget = true; // Items may require a target (for healing, etc.)
            TargetType = TargetType.Friend; // Assume using items on allies
        }

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Check if the performer has the selected item (optional logic to check inventory)
            if (selectedItem == null)
            {
                Debug.Log("No item selected!");
                yield break;
            }

            // Use the action sequence to define the order of using the item
            if (actionSequence != null)
            {
                yield return actionSequence.Execute(performer, target);
            }
        }
    }
}