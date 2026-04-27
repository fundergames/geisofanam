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

using System.Collections;
using Geis.SoulRealm;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// Animates uniform <see cref="Transform.localScale"/> so the VFX mesh diameter matches
    /// start/end radii (meters), using <see cref="unitDiameter"/> at scale 1.
    /// </summary>
    public sealed class BossSlamShockwaveVfx : MonoBehaviour
    {
        [Tooltip("Outer diameter in meters when localScale is (1,1,1). Used to map radius to scale.")]
        [SerializeField] private float unitDiameter = 1f;

        [Tooltip("Normalized time (0–1) → blend from start radius to end radius.")]
        [SerializeField] private AnimationCurve scaleOverNormalizedTime = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Seconds after expansion ends before the instance is destroyed. Use ≥1s for particle bursts that outlast the scale-up.")]
        [SerializeField] private float destroyAfterExpandDelay = 1.5f;

        /// <param name="startRadiusMeters">Initial ring/sphere-equivalent radius (world scale at parent lossyScale 1).</param>
        /// <param name="endRadiusMeters">Final radius after expansion.</param>
        public void Play(float startRadiusMeters, float endRadiusMeters, float durationSeconds)
        {
            StopAllCoroutines();
            StartCoroutine(PlayRoutine(startRadiusMeters, endRadiusMeters, durationSeconds));
        }

        private IEnumerator PlayRoutine(float startRadiusMeters, float endRadiusMeters, float durationSeconds)
        {
            float d = Mathf.Max(0.0001f, unitDiameter);
            float startScale = (2f * Mathf.Max(0f, startRadiusMeters)) / d;
            float endScale = (2f * Mathf.Max(0f, endRadiusMeters)) / d;

            durationSeconds = Mathf.Max(0.01f, durationSeconds);
            float t = 0f;
            while (t < durationSeconds)
            {
                t += RealmSimulation.DeltaTime(RealmSimulationGroup.Physical);
                float u = Mathf.Clamp01(t / durationSeconds);
                float k = scaleOverNormalizedTime.Evaluate(u);
                float s = Mathf.Lerp(startScale, endScale, k);
                transform.localScale = new Vector3(s, s, s);
                yield return null;
            }

            transform.localScale = Vector3.one * endScale;
            Destroy(gameObject, destroyAfterExpandDelay);
        }
    }
}
