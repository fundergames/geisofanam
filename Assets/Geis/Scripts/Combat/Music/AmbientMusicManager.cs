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

// Geis of Anam - Ambient background music.

using UnityEngine;

namespace Geis.Combat.Music
{
    /// <summary>
    /// Manages ambient background music. Can use a direct clip or WorldDefinition.backgroundMusic for level-specific ambient.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbientMusicManager : MonoBehaviour
    {
        [Header("Music")]
        [Tooltip("Ambient clip to play. Used if no World Definition is set.")]
        [SerializeField] private AudioClip ambientClip;

        [Tooltip("Optional: use this world's backgroundMusic instead of Ambient Clip.")]
        [SerializeField] private RogueDeal.Levels.WorldDefinition worldDefinition;

        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;

            AudioClip clip = ambientClip;
            if (worldDefinition != null && worldDefinition.backgroundMusic != null)
                clip = worldDefinition.backgroundMusic;

            if (clip != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
            }
            else if (ambientClip == null && (worldDefinition == null || worldDefinition.backgroundMusic == null))
            {
                Debug.LogWarning("[AmbientMusicManager] No ambient clip assigned. Assign Ambient Clip or World Definition with backgroundMusic.");
            }
        }

        /// <summary>
        /// Set the ambient clip at runtime (e.g. when loading a new level).
        /// </summary>
        public void SetAmbientClip(AudioClip clip)
        {
            if (clip == null) return;
            _audioSource.clip = clip;
            _audioSource.Play();
        }

        /// <summary>
        /// Set ambient from WorldDefinition (uses backgroundMusic).
        /// </summary>
        public void SetWorld(RogueDeal.Levels.WorldDefinition world)
        {
            worldDefinition = world;
            if (world != null && world.backgroundMusic != null && _audioSource != null)
            {
                _audioSource.clip = world.backgroundMusic;
                _audioSource.Play();
            }
        }
    }
}
