/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

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
