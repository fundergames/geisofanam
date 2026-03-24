using UnityEngine;

namespace RogueDeal.Combat
{
    public class DamagePopupManager : MonoBehaviour
    {
        [Header("Pooling Settings")]
        [SerializeField] private GameObject damagePopupPrefab;
        [SerializeField] private Transform popupParent;
        [SerializeField] private bool usePooling = true;
        [SerializeField] private int poolInitialSize = 20;
        [SerializeField] private int poolMaxSize = 100;

        private static DamagePopupManager instance;
        private DamagePopupPool damagePopupPool;

        public static DamagePopupManager Instance => instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (usePooling && damagePopupPrefab != null)
            {
                InitializePool();
            }
        }

        private void InitializePool()
        {
            Transform poolParent = popupParent != null ? popupParent : transform;

            damagePopupPool = new DamagePopupPool(
                prefab: damagePopupPrefab,
                parent: poolParent,
                initialSize: poolInitialSize,
                maxSize: poolMaxSize
            );

            Debug.Log($"[DamagePopupManager] Initialized pool with {poolInitialSize} popups (max: {poolMaxSize})");
        }

        public void ShowDamagePopup(int damage, bool isCritical, Vector3 worldPosition)
        {
            if (damagePopupPrefab == null)
            {
                Debug.LogWarning("[DamagePopupManager] DamagePopup prefab is not assigned!");
                return;
            }

            GameObject popupObj;

            if (usePooling && damagePopupPool != null)
            {
                popupObj = damagePopupPool.Get();
            }
            else
            {
                popupObj = Instantiate(damagePopupPrefab, popupParent != null ? popupParent : transform);
            }

            if (popupObj != null)
            {
                DamagePopup popup = popupObj.GetComponent<DamagePopup>();
                if (popup != null)
                {
                    popup.Initialize(damage, isCritical, worldPosition, usePooling ? damagePopupPool : null);
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
