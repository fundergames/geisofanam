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
        private float maxHealthCached;

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
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (followTarget == null)
            {
                if (alwaysFaceCamera && mainCamera != null)
                    ApplyBillboardRotation();
                return;
            }

            // Moving the same transform as followTarget would drag the whole enemy — only offset a child canvas.
            if (transform != followTarget)
                transform.position = followTarget.position + offset;

            // Never rotate the follow target's transform — only the child canvas should billboard, or the whole enemy spins toward the camera.
            if (alwaysFaceCamera && transform != followTarget)
                ApplyBillboardRotation();
        }

        /// <summary>
        /// Aligns world-space UI with the camera view plane (stable billboard, no inside-out quirk from forward-copy).
        /// </summary>
        private void ApplyBillboardRotation()
        {
            if (mainCamera == null)
                return;
            transform.rotation = mainCamera.transform.rotation;
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        public void UpdateHealthBar(int currentHealth, int maxHealth, bool animate = true)
        {
            UpdateHealthBar((float)currentHealth, (float)maxHealth, animate);
        }

        public void UpdateHealthBar(float currentHealth, float maxHealth, bool animate = true)
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
                        value =>
                        {
                            startValue = value;
                            healthBarSlider.value = value;
                        },
                        currentHealth,
                        healthAnimationDuration
                    ).SetEase(healthAnimationEase);

                    AnimateHealthText(startValue, currentHealth);
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

        private void AnimateHealthText(float startHealth, float targetHealth)
        {
            DOTween.To(
                () => startHealth,
                value => {
                    currentHealthDisplay = Mathf.RoundToInt(value);
                    UpdateHealthText(currentHealthDisplay, Mathf.RoundToInt(maxHealthCached));
                },
                targetHealth,
                healthAnimationDuration
            ).SetEase(healthAnimationEase);
        }

        private void UpdateHealthText(float currentHealth, float maxHealth)
        {
            if (healthText != null)
            {
                healthText.text = $"{Mathf.RoundToInt(currentHealth)} / {Mathf.RoundToInt(maxHealth)}";
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
