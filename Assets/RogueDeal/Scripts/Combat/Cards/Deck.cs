using System.Collections.Generic;
using Funder.Core.Randoms;
using UnityEngine;

namespace RogueDeal.Combat.Cards
{
    public class Deck
    {
        private readonly List<Card> allCards = new List<Card>();
        private readonly IRandomHub _randomHub;
        private readonly string _deckId;
        private int wildCardCount = 0;

        public int TotalCardCount => allCards.Count;
        public int WildCardCount => wildCardCount;

        public Deck(IRandomHub randomHub, string deckId = "Default")
        {
            _randomHub = randomHub ?? throw new System.ArgumentNullException(nameof(randomHub));
            _deckId = deckId;
            InitializeStandardDeck();
        }

        private void InitializeStandardDeck()
        {
            allCards.Clear();
            
            foreach (CardSuit suit in System.Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardRank rank in System.Enum.GetValues(typeof(CardRank)))
                {
                    allCards.Add(new Card(suit, rank));
                }
            }
        }

        public void AddWildCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                allCards.Add(new Card(CardSuit.Hearts, CardRank.Ace, true));
                wildCardCount++;
            }
        }

        public Card DrawRandomCard()
        {
            if (allCards.Count == 0)
            {
                Debug.LogWarning("Deck is empty! Reinitializing...");
                InitializeStandardDeck();
            }

            var stream = _randomHub.GetStream($"Combat/Cards/{_deckId}");
            int index = stream.NextInt(0, allCards.Count);
            return allCards[index].Clone();
        }

        public List<Card> DrawHand(int cardCount = 5)
        {
            var hand = new List<Card>();
            for (int i = 0; i < cardCount; i++)
            {
                hand.Add(DrawRandomCard());
            }
            return hand;
        }

        public void Reset()
        {
            InitializeStandardDeck();
            wildCardCount = 0;
        }
    }
}
