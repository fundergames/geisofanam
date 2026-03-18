using System;

namespace RogueDeal.Combat.Cards
{
    [Serializable]
    public class Card
    {
        public CardSuit suit;
        public CardRank rank;
        public bool isWild;

        public Card(CardSuit suit, CardRank rank, bool isWild = false)
        {
            this.suit = suit;
            this.rank = rank;
            this.isWild = isWild;
        }

        public int GetNumericValue()
        {
            return rank switch
            {
                CardRank.Ace => 14,
                CardRank.King => 13,
                CardRank.Queen => 12,
                CardRank.Jack => 11,
                _ => (int)rank
            };
        }

        public override string ToString()
        {
            if (isWild)
                return "Wild Card";
            return $"{rank} of {suit}";
        }

        public Card Clone()
        {
            return new Card(suit, rank, isWild);
        }
    }

    public enum CardSuit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    public enum CardRank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }
}
