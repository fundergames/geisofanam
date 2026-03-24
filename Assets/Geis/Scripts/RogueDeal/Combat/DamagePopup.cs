using UnityEngine;
using TMPro;
using DG.Tweening;
using RogueDeal.UI;

namespace RogueDeal.Combat
{
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float moveDuration = 1f;
        [SerializeField] private float moveDistance = 1f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color criticalColor = Color.yellow;
        [SerializeField] private bool billboardToCamera = true;

        private Camera mainCamera;
        private DamagePopupPool pool;

        public void Initialize(int damage, bool isCritical, Vector3 worldPosition, DamagePopupPool poolReference = null)
        {
            if (damageText == null)
                damageText = GetComponentInChildren<TextMeshProUGUI>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (billboardToCamera)
                mainCamera = Camera.main;

            pool = poolReference;

            damageText.text = damage.ToString();
            damageText.color = isCritical ? criticalColor : normalColor;

            transform.position = worldPosition;

            canvasGroup.alpha = 1f;

            gameObject.SetActive(true);

            AnimatePopup();
        }

        private void LateUpdate()
        {
            if (billboardToCamera && mainCamera != null)
            {
                transform.rotation = mainCamera.transform.rotation;
            }
        }

        private void AnimatePopup()
        {
            Vector3 targetPosition = transform.position + Vector3.up * moveDistance;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(targetPosition, moveDuration).SetEase(Ease.OutQuad));
            sequence.Join(canvasGroup.DOFade(0f, moveDuration).SetEase(Ease.InQuad));
            sequence.OnComplete(OnAnimationComplete);
        }

        private void OnAnimationComplete()
        {
            if (pool != null)
            {
                pool.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            DOTween.Kill(transform);
            DOTween.Kill(canvasGroup);
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
            DOTween.Kill(canvasGroup);
            mainCamera = null;
        }
    }
}
