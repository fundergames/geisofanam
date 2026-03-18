using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace RogueDeal.Combat
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Slider healthBarSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);
        [SerializeField] private bool alwaysFaceCamera = true;

        [Header("Animation Settings")]
        [SerializeField] private float healthAnimationDuration = 0.3f;
        [SerializeField] private Ease healthAnimationEase = Ease.OutQuad;

        private Camera mainCamera;
        private RectTransform rectTransform;
        private Tweener healthTweener;
        private int currentHealthDisplay;
        private int maxHealthCached;

        private void Awake()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
            
            EnsureCorrectScale();
        }

        private void EnsureCorrectScale()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && rectTransform != null)
            {
                if (rectTransform.localScale.x > 0.1f || rectTransform.localScale.y > 0.1f)
                {
                    rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    Debug.Log($"Auto-fixed health bar scale for {gameObject.name}");
                }
            }
        }

        private void LateUpdate()
        {
            if (followTarget != null)
            {
                transform.position = followTarget.position + offset;

                if (alwaysFaceCamera && mainCamera != null)
                {
                    transform.forward = mainCamera.transform.forward;
                }
            }
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void UpdateHealthBar(int currentHealth, int maxHealth, bool animate = true)
        {
            maxHealthCached = maxHealth;

            if (healthBarSlider != null)
            {
                healthBarSlider.maxValue = maxHealth;

                if (animate && Application.isPlaying)
                {
                    healthTweener?.Kill();

                    float startValue = healthBarSlider.value;
                    healthTweener = DOTween.To(
                        () => startValue,
                        value => healthBarSlider.value = value,
                        currentHealth,
                        healthAnimationDuration
                    ).SetEase(healthAnimationEase);

                    AnimateHealthText(Mathf.RoundToInt(startValue), currentHealth);
                }
                else
                {
                    healthBarSlider.value = currentHealth;
                    UpdateHealthText(currentHealth, maxHealth);
                }
            }
            else
            {
                UpdateHealthText(currentHealth, maxHealth);
            }
        }

        private void AnimateHealthText(int startHealth, int targetHealth)
        {
            DOTween.To(
                () => startHealth,
                value => {
                    currentHealthDisplay = value;
                    UpdateHealthText(currentHealthDisplay, maxHealthCached);
                },
                targetHealth,
                healthAnimationDuration
            ).SetEase(healthAnimationEase);
        }

        private void UpdateHealthText(int currentHealth, int maxHealth)
        {
            if (healthText != null)
            {
                healthText.text = $"{currentHealth} / {maxHealth}";
            }
        }

        public void SetOffset(Vector3 newOffset)
        {
            offset = newOffset;
        }

        private void OnDestroy()
        {
            healthTweener?.Kill();
            DOTween.Kill(this);
        }
    }
}
