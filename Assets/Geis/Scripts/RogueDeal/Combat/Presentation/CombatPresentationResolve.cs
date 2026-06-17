/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System.Collections.Generic;
using Geis.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Resolved presentation cue ready for <see cref="CombatPresentationScheduler"/>.
    /// </summary>
    public struct CombatPresentationCue
    {
        public float timeSeconds;
        public string eventName;
        public AudioClip sfx;
        public GameObject vfxPrefab;
        public CombatCameraShakeSpec cameraShake;
        public CombatHitStopSpec hitStop;
    }

    /// <summary>
    /// Resolves combo-step presentation events or CombatAction.effectBindings into scheduled cues.
    /// </summary>
    public static class CombatPresentationResolve
    {
        public static bool TryResolve(
            GeisComboData comboData,
            int comboState,
            CombatAction action,
            out CombatPresentationCue[] cues)
        {
            cues = null;
            if (comboData != null && comboData.TryGetPresentationEvents(comboState, out GeisComboPresentationEvent[] comboEvents))
            {
                float clipLen = comboData.GetClipLengthSeconds(comboState);
                return BuildFromComboEvents(comboEvents, clipLen, out cues);
            }

            if (action?.effectBindings == null || action.effectBindings.Length == 0)
                return false;

            float fallbackLen = 1f;
            if (comboData != null)
                fallbackLen = comboData.GetClipLengthSeconds(comboState);
            else if (action.comboAnimations != null && action.comboAnimations.Length > 0 && action.comboAnimations[0] != null)
                fallbackLen = action.comboAnimations[0].length;

            return BuildFromEffectBindings(action.effectBindings, fallbackLen, out cues);
        }

        public static bool HasAnyPresentationOrImpact(GeisComboData comboData, int comboState, CombatAction action)
        {
            if (TryResolve(comboData, comboState, action, out CombatPresentationCue[] presentation) && presentation.Length > 0)
                return true;
            return comboData != null && comboData.TryGetImpactFeelCues(comboState, out CombatImpactFeelCue[] impact) && impact.Length > 0;
        }

        public static bool BuildFromComboEvents(
            GeisComboPresentationEvent[] events,
            float clipLengthSeconds,
            out CombatPresentationCue[] cues)
        {
            cues = null;
            if (events == null || events.Length == 0)
                return false;

            clipLengthSeconds = Mathf.Max(clipLengthSeconds, 0.001f);
            var list = new List<CombatPresentationCue>(events.Length);
            for (int i = 0; i < events.Length; i++)
            {
                GeisComboPresentationEvent e = events[i];
                if (e == null || !CueHasContent(e.sfx, e.vfxPrefab, e.cameraShake, e.hitStop))
                    continue;

                list.Add(new CombatPresentationCue
                {
                    timeSeconds = Mathf.Clamp01(e.normalizedTime) * clipLengthSeconds,
                    eventName = e.eventName,
                    sfx = e.sfx,
                    vfxPrefab = e.vfxPrefab,
                    cameraShake = e.cameraShake,
                    hitStop = e.hitStop
                });
            }

            if (list.Count == 0)
                return false;

            list.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
            cues = list.ToArray();
            return true;
        }

        public static bool BuildFromEffectBindings(
            EffectBinding[] bindings,
            float clipLengthSeconds,
            out CombatPresentationCue[] cues)
        {
            cues = null;
            if (bindings == null || bindings.Length == 0)
                return false;

            clipLengthSeconds = Mathf.Max(clipLengthSeconds, 0.001f);
            var list = new List<CombatPresentationCue>(bindings.Length);
            for (int i = 0; i < bindings.Length; i++)
            {
                EffectBinding b = bindings[i];
                if (b == null || !CueHasContent(b.sfx, b.vfxPrefab, b.cameraShake, b.hitStop))
                    continue;

                list.Add(new CombatPresentationCue
                {
                    timeSeconds = Mathf.Clamp01(b.normalizedTime) * clipLengthSeconds,
                    eventName = b.eventName,
                    sfx = b.sfx,
                    vfxPrefab = b.vfxPrefab,
                    cameraShake = b.cameraShake,
                    hitStop = b.hitStop
                });
            }

            if (list.Count == 0)
                return false;

            list.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));
            cues = list.ToArray();
            return true;
        }

        private static bool CueHasContent(
            AudioClip sfx,
            GameObject vfx,
            CombatCameraShakeSpec shake,
            CombatHitStopSpec hitStop)
        {
            return sfx != null || vfx != null || shake.enabled || hitStop.enabled;
        }
    }
}
