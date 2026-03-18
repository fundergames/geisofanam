using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatAnimationEventReceiver : MonoBehaviour
    {
        private CombatEntity combatEntity;
        private CombatEventData pendingAttack;

        private void Awake()
        {
            combatEntity = GetComponent<CombatEntity>();
        }

        public void PrepareAttack(CombatEventData attackData)
        {
            pendingAttack = attackData;
        }

        public void OnAttackHitFrame()
        {
            if (pendingAttack != null)
            {
                CombatEvents.TriggerAttackConnected(pendingAttack);
            }
        }

        public void OnAttackComplete()
        {
            if (pendingAttack != null)
            {
                CombatEvents.TriggerAttackCompleted(pendingAttack);
                pendingAttack = null;
            }
        }

        public void OnFootstep()
        {
        }

        public void OnWeaponSwing()
        {
        }
    }
}
