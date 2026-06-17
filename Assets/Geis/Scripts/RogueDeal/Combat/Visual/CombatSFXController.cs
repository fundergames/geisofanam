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
    public class CombatSFXController : MonoBehaviour
    {
        [Header("Audio Source")]
        [SerializeField] private AudioSource audioSource;
        
        [Header("Hit Sounds")]
        [SerializeField] private AudioClip normalHitSound;
        [SerializeField] private AudioClip criticalHitSound;
        [SerializeField] private AudioClip blockSound;
        [SerializeField] private AudioClip dodgeSound;

        private void Awake()
        {
            EnsureAudioSource();
        }

        /// <summary>Called by <see cref="Presentation.CombatPresentationRuntimeSetup"/> when it creates a source.</summary>
        public void BindAudioSource(AudioSource source)
        {
            audioSource = source;
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public bool PlayAbilitySFX(AudioClip clip)
        {
            if (clip == null)
                return false;

            EnsureAudioSource();
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
                return true;
            }

            AudioSource.PlayClipAtPoint(clip, transform.position);
            return true;
        }

        public void PlayHitSFX(EffectType effectType)
        {
            if (audioSource == null) return;

            switch (effectType)
            {
                case EffectType.Damage:
                    if (normalHitSound != null)
                        audioSource.PlayOneShot(normalHitSound);
                    break;
            }
        }

        public void PlayBlockSFX()
        {
            if (audioSource != null && blockSound != null)
                audioSource.PlayOneShot(blockSound);
        }

        public void PlayDodgeSFX()
        {
            if (audioSource != null && dodgeSound != null)
                audioSource.PlayOneShot(dodgeSound);
        }

    }
}
