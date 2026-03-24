using RogueDeal.Events;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace RogueDeal.UI
{
    public class ComboDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private Vector3 punchScale = new Vector3(0.2f, 0.2f, 0.2f);

        private Sequence currentSequence;

        private void Start()
        {
            EventBus<PlayerAttackEvent>.Subscribe(OnPlayerAttack);
            
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            EventBus<PlayerAttackEvent>.Unsubscribe(OnPlayerAttack);
            
            if (currentSequence != null && currentSequence.IsActive())
                currentSequence.Kill();
        }

        private void OnPlayerAttack(PlayerAttackEvent evt)
        {
            if (evt.totalHits > 1)
            {
                ShowCombo(evt.hitNumber, evt.totalHits, evt.isCrit);
            }
        }

        private void ShowCombo(int hitNumber, int totalHits, bool isCrit)
        {
            if (comboText == null || canvasGroup == null)
                return;

            if (currentSequence != null && currentSequence.IsActive())
                currentSequence.Kill();

            string comboMessage = GetComboMessage(hitNumber, totalHits, isCrit);
            comboText.text = comboMessage;

            currentSequence = DOTween.Sequence();
            currentSequence.Append(canvasGroup.DOFade(1f, fadeInDuration));
            
            if (transform is RectTransform rectTransform)
            {
                float scalePunch = isCrit ? punchScale.x * 1.5f : punchScale.x;
                currentSequence.Join(rectTransform.DOPunchScale(new Vector3(scalePunch, scalePunch, scalePunch), fadeInDuration, 5, 0.5f));
            }
            
            if (hitNumber == totalHits)
            {
                currentSequence.AppendInterval(displayDuration);
                currentSequence.Append(canvasGroup.DOFade(0f, fadeOutDuration));
            }
            else
            {
                currentSequence.AppendInterval(0.3f);
                currentSequence.Append(canvasGroup.DOFade(0f, 0.1f));
            }
        }

        private string GetComboMessage(int hitNumber, int totalHits, bool isCrit)
        {
            string critSuffix = isCrit ? " CRIT!" : "";
            
            if (hitNumber == 1)
            {
                return GetComboName(totalHits) + critSuffix;
            }
            else if (hitNumber == totalHits)
            {
                return $"{totalHits}-HIT COMBO!{critSuffix}";
            }
            else
            {
                return $"HIT {hitNumber}!{critSuffix}";
            }
        }

        private string GetComboName(int hits)
        {
            return hits switch
            {
                2 => "DOUBLE STRIKE!",
                3 => "TRIPLE COMBO!",
                4 => "QUAD ATTACK!",
                5 => "ULTIMATE COMBO!",
                _ => $"{hits}-HIT ATTACK!"
            };
        }
    }
}
