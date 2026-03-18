using UnityEngine;
using TMPro;

namespace RogueDeal.Combat.UI
{
    public class DamageNumberSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Transform damageNumberParent;
        [SerializeField] private Camera mainCamera;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += HandleDamageApplied;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
        }

        private void HandleDamageApplied(CombatEventData data)
        {
            SpawnDamageNumber(data.hitPosition, data.damageAmount, data.wasCritical);
        }

        private void SpawnDamageNumber(Vector3 worldPosition, float damage, bool isCritical)
        {
            if (damageNumberPrefab == null || damageNumberParent == null) return;

            GameObject numberObj = Instantiate(damageNumberPrefab, damageNumberParent);
            DamageNumber damageNumber = numberObj.GetComponent<DamageNumber>();
            
            if (damageNumber != null)
            {
                damageNumber.Initialize(damage, isCritical, worldPosition, mainCamera);
            }
        }
    }
}
