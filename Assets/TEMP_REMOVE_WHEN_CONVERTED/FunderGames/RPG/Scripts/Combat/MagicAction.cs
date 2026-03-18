using System.Collections;
using UnityEngine;

namespace FunderGames.RPG
{
    [CreateAssetMenu(fileName = "MagicAction", menuName = "FunderGames/CombatActions/MagicAction")]
    public class MagicAction : CombatAction
    {
        public int manaCost;
        public int magicDamage;
        public ActionStep castAnimationStep; // Reference to an animation step
        public ActionStep manaDeductionStep; // Reference to a mana deduction step
        public ActionStep projectileStep; // Optional step for projectile effect
        public ActionStep damageStep; // Reference to a damage step

        public MagicAction()
        {
            ActionName = "Cast Spell";
            ActionType = ActionType.Magic;
            RequiresTarget = true;
            TargetType = TargetType.Enemy;
        }

        public override IEnumerator Execute(Combatant performer, Combatant target)
        {
            // Check if the performer has enough mana to cast the spell
            if (performer.Mana < manaCost)
            {
                Debug.Log("Not enough mana!");
                yield break;
            }

            // Use the action sequence to define the order of the magic casting
            if (actionSequence != null)
            {
                yield return actionSequence.Execute(performer, target);
            }
        }
    }
}