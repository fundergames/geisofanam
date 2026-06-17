/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Shared playback for <see cref="EffectBinding"/> and <see cref="CombatPresentationCue"/>.
    /// </summary>
    public static class CombatEffectBindingPlayer
    {
        public static void PlayCue(
            CombatPresentationCue cue,
            CombatVFXController vfxController,
            CombatSFXController sfxController,
            Vector3 vfxPosition,
            Transform fallbackTransform)
        {
            if (cue.vfxPrefab != null)
            {
                // Spawn pose is world-space at vfxPosition + fallback rotation; weapon-aligned polish is a follow-up.
                if (vfxController != null)
                    vfxController.PlayAbilityVFX(cue.vfxPrefab, vfxPosition);
                else if (fallbackTransform != null)
                    Object.Instantiate(cue.vfxPrefab, vfxPosition, fallbackTransform.rotation);
            }

            if (cue.sfx != null)
            {
                bool played = sfxController != null && sfxController.PlayAbilitySFX(cue.sfx);
                if (!played && fallbackTransform != null)
                    AudioSource.PlayClipAtPoint(cue.sfx, fallbackTransform.position);
            }
        }

        public static void PlayBinding(
            EffectBinding binding,
            CombatVFXController vfxController,
            CombatSFXController sfxController,
            Vector3 vfxPosition,
            Transform fallbackTransform)
        {
            if (binding == null)
                return;

            PlayCue(
                new CombatPresentationCue
                {
                    eventName = binding.eventName,
                    sfx = binding.sfx,
                    vfxPrefab = binding.vfxPrefab
                },
                vfxController,
                sfxController,
                vfxPosition,
                fallbackTransform);
        }

        public static void PlayBindingByEventName(
            string eventName,
            EffectBinding[] bindings,
            CombatVFXController vfxController,
            CombatSFXController sfxController,
            Vector3 vfxPosition,
            Transform fallbackTransform)
        {
            if (string.IsNullOrEmpty(eventName) || bindings == null)
                return;

            for (int i = 0; i < bindings.Length; i++)
            {
                EffectBinding binding = bindings[i];
                if (binding != null && binding.eventName == eventName)
                {
                    PlayBinding(binding, vfxController, sfxController, vfxPosition, fallbackTransform);
                    return;
                }
            }
        }
    }
}
