using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "AttackAction", menuName = "FunderGames/CombatActions/AttackAction")]
    public class AttackAction : CombatAction
    {
        public AttackAction()
        {
            ActionName = "Attack";
            ActionType = ActionType.Attack;
            RequiresTarget = true;
            TargetType = TargetType.Enemy;
        }

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Use the action sequence to define the order of events (move -> attack -> deal damage)
            yield return base.Execute(performer, target);
        }
    }
}