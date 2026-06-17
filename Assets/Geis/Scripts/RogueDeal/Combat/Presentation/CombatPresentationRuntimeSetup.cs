/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Ensures presentation components exist on combat entities (player/enemy prefabs may omit them until baked).
    /// </summary>
    public static class CombatPresentationRuntimeSetup
    {
        public static CombatPresentationScheduler EnsureOn(GameObject combatRoot)
        {
            if (combatRoot == null)
                return null;

            if (combatRoot.GetComponent<CombatVFXController>() == null)
                combatRoot.AddComponent<CombatVFXController>();

            CombatSFXController sfx = combatRoot.GetComponent<CombatSFXController>();
            if (sfx == null)
                sfx = combatRoot.AddComponent<CombatSFXController>();

            EnsureAbilityAudioSource(combatRoot, sfx);

            CombatPresentationScheduler scheduler = combatRoot.GetComponent<CombatPresentationScheduler>();
            if (scheduler == null)
                scheduler = combatRoot.AddComponent<CombatPresentationScheduler>();

            CombatHitStopService.FindOrCreateOn(combatRoot);

            return scheduler;
        }

        public static AudioSource EnsureAbilityAudioSource(GameObject combatRoot, CombatSFXController sfxController)
        {
            AudioSource source = sfxController != null ? sfxController.GetComponent<AudioSource>() : null;
            if (source == null && sfxController != null)
                source = sfxController.gameObject.GetComponent<AudioSource>();

            if (source == null && combatRoot != null)
                source = combatRoot.GetComponent<AudioSource>();

            if (source == null && combatRoot != null)
            {
                source = combatRoot.AddComponent<AudioSource>();
                ConfigureAbilityAudioSource(source);
            }
            else if (source != null)
            {
                ConfigureAbilityAudioSource(source);
            }

            if (sfxController != null)
                sfxController.BindAudioSource(source);

            return source;
        }

        public static void ConfigureAbilityAudioSource(AudioSource source)
        {
            if (source == null)
                return;

            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
            source.volume = 1f;
        }
    }
}
