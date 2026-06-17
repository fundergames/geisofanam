/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    public static class CombatPresentationFeelPlayer
    {
        public static void ApplyCueFeel(CombatPresentationCue cue, int attackToken, Animator attackerAnimator, GameObject combatRoot)
        {
            if (cue.cameraShake.enabled)
            {
                CombatCameraShake shake = CombatCameraShake.FindOrCreate();
                shake?.RequestShake(cue.cameraShake);
            }

            if (cue.hitStop.enabled)
            {
                CombatHitStopService service = CombatHitStopService.FindOrCreateOn(combatRoot);
                service?.Push(attackToken, cue.hitStop, attackerAnimator);
            }
        }

        public static void ApplyImpactFeel(
            CombatCameraShakeSpec cameraShake,
            CombatHitStopSpec hitStop,
            int attackToken,
            Animator attackerAnimator,
            GameObject combatRoot)
        {
            if (cameraShake.enabled)
            {
                CombatCameraShake shake = CombatCameraShake.FindOrCreate();
                shake?.RequestShake(cameraShake);
            }

            if (hitStop.enabled)
            {
                CombatHitStopService service = CombatHitStopService.FindOrCreateOn(combatRoot);
                service?.Push(attackToken, hitStop, attackerAnimator);
            }
        }

        public static void CancelAttackFeel(int attackToken, GameObject combatRoot)
        {
            if (combatRoot != null)
            {
                CombatHitStopService service = combatRoot.GetComponent<CombatHitStopService>();
                service?.CancelToken(attackToken);
            }
        }
    }
}
