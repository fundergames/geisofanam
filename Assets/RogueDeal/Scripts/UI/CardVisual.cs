using DG.Tweening;
using RogueDeal.Combat.Cards;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RogueDeal.UI
{
    public class CardVisual : MonoBehaviour, IPointerClickHandler
    {
        [Header("Visual Components")]
        [SerializeField] private Image cardFrame;
        [SerializeField] private Image cardBack;
        [SerializeField] private Image suitIcon;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private Image highlightGlow;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Card Sprites")]
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private Sprite diamondSprite;
        [SerializeField] private Sprite clubSprite;
        [SerializeField] private Sprite spadeSprite;
        [SerializeField] private Sprite wildCardSprite;
        
        [Header("Colors")]
        [SerializeField] private Color redSuitColor = Color.red;
        [SerializeField] private Color blackSuitColor = Color.black;
        [SerializeField] private Color wildCardColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color heldColor = new Color(1f, 1f, 0.5f);
        [SerializeField] private Color winningColor = new Color(0f, 1f, 0.5f);
        
        private Card cardData;
        private int cardIndex;
        private bool isHeld = false;
        private bool isFaceUp = false;
        private bool isInteractable = true;
        private float targetZRotation = 0f;
        
        public event Action<int> OnCardClicked;
        
        public Card CardData => cardData;
        public int CardIndex => cardIndex;
        public bool IsHeld => isHeld;
        public bool IsFaceUp => isFaceUp;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                
            if (highlightGlow != null)
                highlightGlow.gameObject.SetActive(false);
        }

        public void Initialize(Card card, int index)
        {
            cardData = card;
            cardIndex = index;
            isHeld = false;
            isFaceUp = false;
            
            UpdateVisuals();
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
        }

        public void SetHeld(bool held)
        {
            isHeld = held;
            UpdateHeldState();
        }

        public void ToggleHeld()
        {
            SetHeld(!isHeld);
        }

        private void UpdateVisuals()
        {
            if (cardBack != null)
                cardBack.gameObject.SetActive(!isFaceUp);

            if (cardFrame != null)
                cardFrame.gameObject.SetActive(isFaceUp);

            if (rankText != null)
                rankText.gameObject.SetActive(isFaceUp);

            if (suitIcon != null)
                suitIcon.gameObject.SetActive(isFaceUp);

            if (!isFaceUp)
                return;

            if (cardData.isWild)
            {
                UpdateWildCard();
            }
            else
            {
                UpdateStandardCard();
            }
        }

        private void UpdateStandardCard()
        {
            if (rankText != null)
                rankText.text = GetRankSymbol(cardData.rank);

            if (suitIcon != null)
            {
                suitIcon.sprite = GetSuitSprite(cardData.suit);
                suitIcon.color = IsRedSuit(cardData.suit) ? redSuitColor : blackSuitColor;
            }

            if (rankText != null)
                rankText.color = IsRedSuit(cardData.suit) ? redSuitColor : blackSuitColor;
        }

        private void UpdateWildCard()
        {
            if (rankText != null)
            {
                rankText.text = "W";
                rankText.color = wildCardColor;
            }

            if (suitIcon != null)
            {
                suitIcon.sprite = wildCardSprite;
                suitIcon.color = wildCardColor;
            }
        }

        private void UpdateHeldState()
        {
            if (highlightGlow != null)
            {
                highlightGlow.gameObject.SetActive(isHeld);
                highlightGlow.color = heldColor;
            }

            if (isHeld)
            {
                transform.DOLocalMoveY(transform.localPosition.y + 20f, 0.3f)
                    .SetEase(Ease.OutBack);
            }
            else
            {
                transform.DOLocalMoveY(transform.localPosition.y - 20f, 0.3f)
                    .SetEase(Ease.InBack);
            }
        }

        public Sequence AnimateDeal(Vector3 startPosition, Vector3 endPosition, float delay, float duration, Quaternion targetRotation = default)
        {
            transform.localPosition = startPosition;
            transform.localRotation = Quaternion.identity;
            canvasGroup.alpha = 0f;
            isFaceUp = false;
            
            if (targetRotation != default(Quaternion))
            {
                targetZRotation = targetRotation.eulerAngles.z;
            }
            else
            {
                targetZRotation = 0f;
            }
            
            UpdateVisuals();

            Sequence sequence = DOTween.Sequence();
            
            sequence.AppendInterval(delay);
            sequence.Append(canvasGroup.DOFade(1f, 0.1f));
            sequence.Join(transform.DOLocalMove(endPosition, duration).SetEase(Ease.OutCubic));
            sequence.Join(transform.DORotate(new Vector3(0, 360, targetZRotation), duration, RotateMode.FastBeyond360));
            
            return sequence;
        }

        public Sequence AnimateFlip(float duration = 0.3f)
        {
            Sequence sequence = DOTween.Sequence();
            
            sequence.Append(transform.DORotate(new Vector3(0, 90, targetZRotation), duration / 2f));
            sequence.AppendCallback(() => {
                isFaceUp = !isFaceUp;
                UpdateVisuals();
            });
            sequence.Append(transform.DORotate(new Vector3(0, 0, targetZRotation), duration / 2f));
            
            return sequence;
        }

        public Sequence AnimateReplace(Card newCard, Vector3 deckPosition, float duration = 0.5f)
        {
            Vector3 originalPosition = transform.localPosition;
            
            Sequence sequence = DOTween.Sequence();
            
            sequence.Append(transform.DOLocalMove(deckPosition, duration / 2f).SetEase(Ease.InCubic));
            sequence.Join(transform.DOScale(0.5f, duration / 2f));
            sequence.Join(canvasGroup.DOFade(0f, duration / 2f));
            
            sequence.AppendCallback(() => {
                cardData = newCard;
                isFaceUp = false;
                UpdateVisuals();
            });
            
            sequence.Append(transform.DOLocalMove(originalPosition, duration / 2f).SetEase(Ease.OutCubic));
            sequence.Join(transform.DOScale(1f, duration / 2f));
            sequence.Join(canvasGroup.DOFade(1f, duration / 2f));
            sequence.Join(AnimateFlip(duration / 2f));
            
            return sequence;
        }

        public Sequence AnimateHighlight(bool isWinning = false)
        {
            if (highlightGlow != null)
            {
                highlightGlow.gameObject.SetActive(true);
                highlightGlow.color = isWinning ? winningColor : heldColor;
            }

            Sequence sequence = DOTween.Sequence();
            
            sequence.Append(transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
            sequence.Join(transform.DOLocalMoveY(transform.localPosition.y + 30f, 0.3f));
            
            if (highlightGlow != null)
            {
                sequence.Join(highlightGlow.DOFade(0.8f, 0.3f).SetLoops(3, LoopType.Yoyo));
            }
            
            return sequence;
        }

        public Sequence AnimateDiscard(Vector3 discardPosition, float duration = 0.4f)
        {
            Sequence sequence = DOTween.Sequence();
            
            sequence.Append(transform.DOLocalMove(discardPosition, duration).SetEase(Ease.InCubic));
            sequence.Join(transform.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-45f, 45f)), duration));
            sequence.Join(transform.DOScale(0f, duration));
            sequence.Join(canvasGroup.DOFade(0f, duration));
            
            return sequence;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || !isFaceUp)
                return;

            ToggleHeld();
            OnCardClicked?.Invoke(cardIndex);
        }

        private string GetRankSymbol(CardRank rank)
        {
            return rank switch
            {
                CardRank.Ace => "A",
                CardRank.Two => "2",
                CardRank.Three => "3",
                CardRank.Four => "4",
                CardRank.Five => "5",
                CardRank.Six => "6",
                CardRank.Seven => "7",
                CardRank.Eight => "8",
                CardRank.Nine => "9",
                CardRank.Ten => "10",
                CardRank.Jack => "J",
                CardRank.Queen => "Q",
                CardRank.King => "K",
                _ => "?"
            };
        }

        private Sprite GetSuitSprite(CardSuit suit)
        {
            return suit switch
            {
                CardSuit.Hearts => heartSprite,
                CardSuit.Diamonds => diamondSprite,
                CardSuit.Clubs => clubSprite,
                CardSuit.Spades => spadeSprite,
                _ => null
            };
        }

        private bool IsRedSuit(CardSuit suit)
        {
            return suit == CardSuit.Hearts || suit == CardSuit.Diamonds;
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);
            DOTween.Kill(cardFrame);
            DOTween.Kill(cardBack);
            DOTween.Kill(suitIcon);
            DOTween.Kill(highlightGlow);
            DOTween.Kill(canvasGroup);
        }
    }
}
