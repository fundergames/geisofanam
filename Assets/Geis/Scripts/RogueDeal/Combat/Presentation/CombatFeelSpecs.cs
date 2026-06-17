/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    public struct CombatImpactFeelCue
    {
        public float timeSeconds;
        public CombatCameraShakeSpec cameraShake;
        public CombatHitStopSpec hitStop;
    }

    public enum CombatHitStopMode
    {
        GlobalTimeScale = 0,
        AttackerAnimatorOnly = 1,
        Both = 2
    }

    [System.Serializable]
    public struct CombatCameraShakeSpec
    {
        public bool enabled;

        [Tooltip("Local-space positional shake magnitude on the camera rig (meters).")]
        [Min(0f)]
        public float amplitude;

        [Tooltip("Shake duration in real seconds.")]
        [Min(0.01f)]
        public float duration;

        [Tooltip("Noise frequency (higher = faster vibration).")]
        [Min(0.1f)]
        public float frequency;

        public static CombatCameraShakeSpec LightImpact => new CombatCameraShakeSpec
        {
            enabled = true,
            amplitude = 0.06f,
            duration = 0.1f,
            frequency = 28f
        };

        public static CombatCameraShakeSpec HeavyImpact => new CombatCameraShakeSpec
        {
            enabled = true,
            amplitude = 0.14f,
            duration = 0.16f,
            frequency = 22f
        };
    }

    [System.Serializable]
    public struct CombatHitStopSpec
    {
        public bool enabled;

        public CombatHitStopMode mode;

        [Tooltip("Time scale while active (Global/Both). Typical impact: 0.05–0.2.")]
        [Range(0.01f, 1f)]
        public float timeScale;

        [Tooltip("Real-time duration before restoring.")]
        [Min(0.01f)]
        public float durationRealSeconds;

        public static CombatHitStopSpec LightImpact => new CombatHitStopSpec
        {
            enabled = true,
            mode = CombatHitStopMode.AttackerAnimatorOnly,
            timeScale = 0.1f,
            durationRealSeconds = 0.04f
        };

        public static CombatHitStopSpec HeavyImpact => new CombatHitStopSpec
        {
            enabled = true,
            mode = CombatHitStopMode.Both,
            timeScale = 0.08f,
            durationRealSeconds = 0.07f
        };
    }
}
