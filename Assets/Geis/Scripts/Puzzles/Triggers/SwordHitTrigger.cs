using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Sword puzzle trigger. Activates after the weapon hitbox sweeps through this zone
    /// <see cref="hitsRequired"/> times. Models the sword "force/break" mechanic:
    /// e.g. a cracked wall needs one charged strike; a reinforced barrier needs three.
    ///
    /// Detection works because <see cref="WeaponHitbox"/> only enables its Collider during
    /// an active swing animation window, so <c>OnTriggerEnter</c> fires only during valid attacks.
    ///
    /// Default realm: PhysicalOnly.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SwordHitTrigger : PuzzleTriggerBase
    {
        [Header("Hit Settings")]
        [Tooltip("Number of sword hits required to activate (1 = single charged strike).")]
        [SerializeField] private int hitsRequired = 1;

        [Header("Visual Feedback")]
        [Tooltip("Optional: materials swapped in order as hit count increases. " +
                 "Index 0 = pristine, index 1 = first hit, etc.")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material[] damageStageMaterials;

        [Header("Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip breakSound;
        [SerializeField] private AudioSource audioSource;

        private int _hitCount;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsActivated) return;
            if (!IsAccessibleInCurrentRealm()) return;

            // Only register hits from an active weapon hitbox (sword swing).
            if (other.GetComponent<WeaponHitbox>() == null) return;

            _hitCount++;
            ApplyDamageStageVisual();

            bool isFinalHit = _hitCount >= hitsRequired;
            PlaySound(isFinalHit ? breakSound : hitSound);

            if (isFinalHit)
                SetActivated(true);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            _hitCount = 0;
            ApplyDamageStageVisual();
        }

        private void ApplyDamageStageVisual()
        {
            if (targetRenderer == null || damageStageMaterials == null || damageStageMaterials.Length == 0)
                return;

            int index = Mathf.Clamp(_hitCount, 0, damageStageMaterials.Length - 1);
            if (damageStageMaterials[index] != null)
                targetRenderer.material = damageStageMaterials[index];
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }
    }
}
