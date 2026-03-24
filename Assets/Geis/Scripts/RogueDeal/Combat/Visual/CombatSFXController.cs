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
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
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

        public void PlayAbilitySFX(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
