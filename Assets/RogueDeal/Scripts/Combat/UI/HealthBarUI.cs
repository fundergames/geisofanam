using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            if (targetEntity != null && targetEntity.stats != null)
            {
                targetEntity.stats.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(targetEntity.stats.CurrentHealth, targetEntity.stats.MaxHealth);
            }
        }

        private void OnDisable()
        {
            if (targetEntity != null && targetEntity.stats != null)
            {
                targetEntity.stats.OnHealthChanged -= UpdateHealthBar;
            }
        }

        private void LateUpdate()
        {
            if (followWorldPosition && targetEntity != null && mainCamera != null)
            {
                Vector3 worldPos = targetEntity.transform.position + worldOffset;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
                transform.position = screenPos;
            }
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
            if (targetEntity != null && targetEntity.stats != null)
            {
                targetEntity.stats.OnHealthChanged -= UpdateHealthBar;
            }

            targetEntity = entity;

            if (targetEntity != null && targetEntity.stats != null)
            {
                targetEntity.stats.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(targetEntity.stats.CurrentHealth, targetEntity.stats.MaxHealth);
            }
        }
    }
}
