using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private CombatEntity targetEntity;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private bool followWorldPosition;
        [SerializeField] private Vector3 worldOffset = Vector3.up * 2f;
        [SerializeField] private Camera mainCamera;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            RefreshHealthFromEntity();
        }

        private void LateUpdate()
        {
            if (targetEntity == null)
                return;

            var data = targetEntity.GetEntityData();
            if (data != null)
                UpdateHealthBar(data.currentHealth, data.maxHealth);

            if (followWorldPosition && mainCamera != null)
            {
                Vector3 worldPos = targetEntity.transform.position + worldOffset;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
                transform.position = screenPos;
            }
        }

        private void RefreshHealthFromEntity()
        {
            if (targetEntity == null)
                return;
            var data = targetEntity.GetEntityData();
            if (data != null)
                UpdateHealthBar(data.currentHealth, data.maxHealth);
        }

        private void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
            }
        }

        public void SetTarget(CombatEntity entity)
        {
            targetEntity = entity;
            RefreshHealthFromEntity();
        }
    }
}
