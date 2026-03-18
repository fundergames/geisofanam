using DG.Tweening;
using Funder.Core.Events;
using RogueDeal.Combat;
using RogueDeal.Combat.Cards;
using RogueDeal.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RogueDeal.UI
{
    public class CardHandUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private CardVisual cardPrefab;
        [SerializeField] private CardLayoutConfig layoutConfig;
        
        [Header("Settings")]
        [HideInInspector] public bool autoHandleEvents = true;
        [SerializeField] private bool autoFlipOnDeal = true;
        [SerializeField] private float autoFlipDelay = 0.5f;
        
        private List<CardVisual> activeCards = new List<CardVisual>();
        private bool isAnimating = false;
        
        public bool IsAnimating => isAnimating;
        public CardVisual CardPrefab => cardPrefab;
        public Transform CardContainer => cardContainer;
        public CardLayoutConfig LayoutConfig => layoutConfig;

        private void Start()
        {
            if (autoHandleEvents)
            {
                EventBus<HandDealtEvent>.Subscribe(OnHandDealt);
                EventBus<HandEvaluatedEvent>.Subscribe(OnHandEvaluated);
            }
        }

        private void OnDestroy()
        {
            if (autoHandleEvents)
            {
                EventBus<HandDealtEvent>.Unsubscribe(OnHandDealt);
                EventBus<HandEvaluatedEvent>.Unsubscribe(OnHandEvaluated);
            }
        }

        public void DealHand(List<Card> cards)
        {
            StartCoroutine(DealHandRoutine(cards));
        }

        private IEnumerator DealHandRoutine(List<Card> cards)
        {
            isAnimating = true;
            
            ClearHand();
            
            Sequence dealSequence = DOTween.Sequence();
            
            for (int i = 0; i < cards.Count; i++)
            {
                CardVisual cardVisual = Instantiate(cardPrefab, cardContainer);
                cardVisual.Initialize(cards[i], i);
                cardVisual.OnCardClicked += OnCardClicked;
                
                Vector3 targetPosition = layoutConfig.GetCardPosition(i, cards.Count);
                Quaternion targetRotation = layoutConfig.GetCardRotation(i, cards.Count);
                
                Vector3 deckWorldPos = layoutConfig.deckPosition;
                
                float delay = i * layoutConfig.dealDelay;
                dealSequence.Join(cardVisual.AnimateDeal(deckWorldPos, targetPosition, delay, layoutConfig.dealDuration, targetRotation));
                
                activeCards.Add(cardVisual);
            }
            
            yield return dealSequence.WaitForCompletion();
            
            if (autoFlipOnDeal)
            {
                yield return new WaitForSeconds(autoFlipDelay);
                yield return FlipAllCards(true);
            }
            
            SetCardsInteractable(true);
            isAnimating = false;
        }

        public IEnumerator FlipAllCards(bool faceUp)
        {
            isAnimating = true;
            
            Sequence flipSequence = DOTween.Sequence();
            
            foreach (var card in activeCards)
            {
                if (card.IsFaceUp != faceUp)
                {
                    flipSequence.Join(card.AnimateFlip(layoutConfig.flipDuration));
                }
            }
            
            yield return flipSequence.WaitForCompletion();
            
            isAnimating = false;
        }

        public IEnumerator ReplaceCards(List<int> indicesToReplace, List<Card> newCards)
        {
            Debug.Log($"[CardHandUI] ReplaceCards called - replacing {indicesToReplace.Count} cards");
            isAnimating = true;
            SetCardsInteractable(false);
            
            Sequence replaceSequence = DOTween.Sequence();
            
            int newCardIndex = 0;
            foreach (int index in indicesToReplace)
            {
                if (index >= 0 && index < activeCards.Count && newCardIndex < newCards.Count)
                {
                    CardVisual cardVisual = activeCards[index];
                    Vector3 deckPos = layoutConfig.deckPosition;
                    
                    Debug.Log($"[CardHandUI] Replacing card at index {index} with {newCards[newCardIndex].rank} of {newCards[newCardIndex].suit}");
                    
                    replaceSequence.Join(
                        cardVisual.AnimateReplace(newCards[newCardIndex], deckPos, layoutConfig.replaceDuration)
                    );
                    
                    newCardIndex++;
                }
            }
            
            yield return replaceSequence.WaitForCompletion();
            
            Debug.Log("[CardHandUI] Replace animation complete");
            isAnimating = false;
        }

        public List<CardVisual> GetActiveCards()
        {
            return new List<CardVisual>(activeCards);
        }

        public IEnumerator HighlightWinningCards(PokerHandType handType, List<Card> currentHand)
        {
            isAnimating = true;
            
            var winningIndices = GetWinningCardIndices(handType, currentHand);
            
            Sequence highlightSequence = DOTween.Sequence();
            
            for (int i = 0; i < activeCards.Count; i++)
            {
                if (winningIndices.Contains(i))
                {
                    highlightSequence.Join(activeCards[i].AnimateHighlight(true));
                }
            }
            
            yield return highlightSequence.WaitForCompletion();
            
            isAnimating = false;
        }

        public void ClearHand()
        {
            foreach (var card in activeCards)
            {
                if (card != null)
                {
                    card.OnCardClicked -= OnCardClicked;
                    Destroy(card.gameObject);
                }
            }
            
            activeCards.Clear();
        }

        public void SetCardsInteractable(bool interactable)
        {
            foreach (var card in activeCards)
            {
                card.SetInteractable(interactable);
            }
        }

        public List<int> GetHeldCardIndices()
        {
            var heldIndices = new List<int>();
            
            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i].IsHeld)
                {
                    heldIndices.Add(i);
                }
            }
            
            return heldIndices;
        }

        public List<bool> GetHeldCardFlags()
        {
            return activeCards.Select(card => card.IsHeld).ToList();
        }

        private void OnCardClicked(int cardIndex)
        {
            Debug.Log($"Card {cardIndex} clicked. Held: {activeCards[cardIndex].IsHeld}");
        }

        private void OnHandDealt(HandDealtEvent evt)
        {
            DealHand(evt.cards);
        }

        private void OnHandEvaluated(HandEvaluatedEvent evt)
        {
            StartCoroutine(OnHandEvaluatedRoutine(evt));
        }

        private IEnumerator OnHandEvaluatedRoutine(HandEvaluatedEvent evt)
        {
            List<Card> currentHand = activeCards.Select(c => c.CardData).ToList();
            
            yield return HighlightWinningCards(evt.handType, currentHand);
            
            yield return new WaitForSeconds(1f);
        }

        private List<int> GetWinningCardIndices(PokerHandType handType, List<Card> hand)
        {
            var indices = new List<int>();
            
            switch (handType)
            {
                case PokerHandType.Pair:
                case PokerHandType.ThreeOfAKind:
                case PokerHandType.FourOfAKind:
                    indices = GetMatchingRankIndices(hand);
                    break;
                    
                case PokerHandType.TwoPair:
                    indices = GetTwoPairIndices(hand);
                    break;
                    
                case PokerHandType.FullHouse:
                    indices = GetFullHouseIndices(hand);
                    break;
                    
                case PokerHandType.Flush:
                case PokerHandType.StraightFlush:
                case PokerHandType.RoyalFlush:
                    for (int i = 0; i < hand.Count; i++)
                        indices.Add(i);
                    break;
                    
                case PokerHandType.Straight:
                    for (int i = 0; i < hand.Count; i++)
                        indices.Add(i);
                    break;
                    
                case PokerHandType.HighCard:
                    indices.Add(GetHighCardIndex(hand));
                    break;
            }
            
            return indices;
        }

        private List<int> GetMatchingRankIndices(List<Card> hand)
        {
            var nonWildCards = hand
                .Select((card, index) => new { card, index })
                .Where(x => !x.card.isWild)
                .ToList();
            
            var wildIndices = hand
                .Select((card, index) => new { card, index })
                .Where(x => x.card.isWild)
                .Select(x => x.index)
                .ToList();
            
            var rankGroups = nonWildCards
                .GroupBy(x => x.card.rank)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Key)
                .FirstOrDefault();
            
            var indices = rankGroups?.Select(x => x.index).ToList() ?? new List<int>();
            indices.AddRange(wildIndices);
            
            return indices;
        }

        private List<int> GetTwoPairIndices(List<Card> hand)
        {
            var nonWildCards = hand
                .Select((card, index) => new { card, index })
                .Where(x => !x.card.isWild)
                .ToList();
            
            var wildIndices = hand
                .Select((card, index) => new { card, index })
                .Where(x => x.card.isWild)
                .Select(x => x.index)
                .ToList();
            
            var pairs = nonWildCards
                .GroupBy(x => x.card.rank)
                .Where(g => g.Count() == 2)
                .OrderByDescending(g => g.Key)
                .Take(2)
                .SelectMany(g => g.Select(x => x.index))
                .ToList();
            
            pairs.AddRange(wildIndices);
            
            return pairs;
        }

        private List<int> GetFullHouseIndices(List<Card> hand)
        {
            var nonWildCards = hand
                .Select((card, index) => new { card, index })
                .Where(x => !x.card.isWild)
                .ToList();
            
            var wildIndices = hand
                .Select((card, index) => new { card, index })
                .Where(x => x.card.isWild)
                .Select(x => x.index)
                .ToList();
            
            var groups = nonWildCards
                .GroupBy(x => x.card.rank)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Key);
            
            var indices = new List<int>();
            foreach (var group in groups)
            {
                indices.AddRange(group.Select(x => x.index));
            }
            
            indices.AddRange(wildIndices);
            
            return indices;
        }

        private int GetHighCardIndex(List<Card> hand)
        {
            int highestIndex = 0;
            int highestValue = hand[0].GetNumericValue();
            
            for (int i = 1; i < hand.Count; i++)
            {
                int value = hand[i].GetNumericValue();
                if (value > highestValue)
                {
                    highestValue = value;
                    highestIndex = i;
                }
            }
            
            return highestIndex;
        }
    }
}
