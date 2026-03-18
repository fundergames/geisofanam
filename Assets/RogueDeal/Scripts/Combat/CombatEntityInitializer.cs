using UnityEngine;
using RogueDeal.Player;

namespace RogueDeal.Combat
{
    public static class CombatEntityInitializer
    {
        public static CombatEntity InitializeEntity(GameObject target, HeroData heroData)
        {
            if (target == null)
            {
                Debug.LogError("[CombatEntityInitializer] Target GameObject is null");
                return null;
            }

            if (heroData == null)
            {
                Debug.LogError("[CombatEntityInitializer] HeroData is null");
                return null;
            }

            CombatEntity entity = target.GetComponent<CombatEntity>();
            if (entity == null)
            {
                entity = target.AddComponent<CombatEntity>();
                Debug.Log($"[CombatEntityInitializer] Added CombatEntity to {target.name}");
            }

            entity.SetHeroData(heroData);

            EnsureAnimationController(target);
            EnsureVFXController(target);
            EnsureSFXController(target);
            EnsureHitPoint(target, entity);
            EnsureVFXSpawnPoint(target, entity);

            var data = entity.GetEntityData();
            Debug.Log($"[CombatEntityInitializer] Initialized CombatEntity for {target.name} (HP: {data?.currentHealth ?? 0})");

            return entity;
        }

        public static CombatEntity InitializePlayer(GameObject playerVisual, HeroData heroData)
        {
            return InitializeEntity(playerVisual, heroData);
        }

        public static CombatEntity InitializeEnemy(GameObject enemyVisual, HeroData enemyData)
        {
            return InitializeEntity(enemyVisual, enemyData);
        }

        private static void EnsureHitPoint(GameObject target, CombatEntity entity)
        {
            if (entity.hitPoint == null)
            {
                Transform existingHitPoint = target.transform.Find("HitPoint");
                
                if (existingHitPoint != null)
                {
                    entity.hitPoint = existingHitPoint;
                }
                else
                {
                    GameObject hitPoint = new GameObject("HitPoint");
                    hitPoint.transform.SetParent(target.transform, false);
                    hitPoint.transform.localPosition = new Vector3(0f, 1.0f, 0f);
                    entity.hitPoint = hitPoint.transform;
                    Debug.Log($"[CombatEntityInitializer] Created HitPoint for {target.name}");
                }
            }
        }

        private static void EnsureVFXSpawnPoint(GameObject target, CombatEntity entity)
        {
            if (entity.vfxSpawnPoint == null)
            {
                Transform existingVFXPoint = target.transform.Find("VFXSpawnPoint");
                
                if (existingVFXPoint != null)
                {
                    entity.vfxSpawnPoint = existingVFXPoint;
                }
                else
                {
                    GameObject vfxPoint = new GameObject("VFXSpawnPoint");
                    vfxPoint.transform.SetParent(target.transform, false);
                    vfxPoint.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                    entity.vfxSpawnPoint = vfxPoint.transform;
                    Debug.Log($"[CombatEntityInitializer] Created VFXSpawnPoint for {target.name}");
                }
            }
        }

        private static void EnsureAnimationController(GameObject target)
        {
            CombatAnimationController animController = target.GetComponent<CombatAnimationController>();
            if (animController == null)
            {
                target.AddComponent<CombatAnimationController>();
                Debug.Log($"[CombatEntityInitializer] Added CombatAnimationController to {target.name}");
            }
        }

        private static void EnsureVFXController(GameObject target)
        {
            CombatVFXController vfxController = target.GetComponent<CombatVFXController>();
            if (vfxController == null)
            {
                target.AddComponent<CombatVFXController>();
                Debug.Log($"[CombatEntityInitializer] Added CombatVFXController to {target.name}");
            }
        }

        private static void EnsureSFXController(GameObject target)
        {
            CombatSFXController sfxController = target.GetComponent<CombatSFXController>();
            if (sfxController == null)
            {
                target.AddComponent<CombatSFXController>();
                Debug.Log($"[CombatEntityInitializer] Added CombatSFXController to {target.name}");
            }
        }

        public static void CleanupEntity(GameObject target)
        {
            if (target == null) return;

            CombatEntity entity = target.GetComponent<CombatEntity>();
            if (entity != null)
            {
                Object.Destroy(entity);
                Debug.Log($"[CombatEntityInitializer] Cleaned up CombatEntity from {target.name}");
            }
        }
    }
}
