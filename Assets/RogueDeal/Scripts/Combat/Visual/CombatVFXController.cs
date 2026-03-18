using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatVFXController : MonoBehaviour
    {
        [Header("Hit VFX")]
        [SerializeField] private GameObject normalHitVFX;
        [SerializeField] private GameObject criticalHitVFX;
        [SerializeField] private GameObject blockVFX;
        [SerializeField] private GameObject dodgeVFX;
        
        [Header("Settings")]
        [SerializeField] private float vfxLifetime = 2f;

        public void PlayHitVFX(Vector3 position, bool isCritical)
        {
            GameObject vfxPrefab = isCritical ? criticalHitVFX : normalHitVFX;
            if (vfxPrefab != null)
            {
                GameObject vfx = Instantiate(vfxPrefab, position, Quaternion.identity);
                Destroy(vfx, vfxLifetime);
            }
        }

        public void PlayBlockVFX(Vector3 position)
        {
            if (blockVFX != null)
            {
                GameObject vfx = Instantiate(blockVFX, position, Quaternion.identity);
                Destroy(vfx, vfxLifetime);
            }
        }

        public void PlayAbilityVFX(GameObject vfxPrefab, Vector3 position)
        {
            if (vfxPrefab != null)
            {
                GameObject vfx = Instantiate(vfxPrefab, position, Quaternion.identity);
                Destroy(vfx, vfxLifetime * 2.5f);
            }
        }
    }
}
