using System.Collections.Generic;
using System.Linq;

namespace RogueDeal.Combat.Cards
{
    public static class PokerHandEvaluator
    {
        public static PokerHandType EvaluateHand(List<Card> hand)
        {
            if (hand == null || hand.Count != 5)
                return PokerHandType.HighCard;

            var sortedHand = hand.OrderByDescending(c => c.GetNumericValue()).ToList();

            bool isFlush = IsFlush(sortedHand);
            bool isStraight = IsStraight(sortedHand);

            if (isFlush && isStraight && sortedHand[0].rank == CardRank.Ace)
                return PokerHandType.RoyalFlush;

            if (isFlush && isStraight)
                return PokerHandType.StraightFlush;

            var rankCounts = GetRankCounts(sortedHand);
            var counts = rankCounts.Values.OrderByDescending(v => v).ToList();

            if (counts.Count > 0 && counts[0] == 4)
                return PokerHandType.FourOfAKind;

            if (counts.Count > 1 && counts[0] == 3 && counts[1] == 2)
                return PokerHandType.FullHouse;

            if (isFlush)
                return PokerHandType.Flush;

            if (isStraight)
                return PokerHandType.Straight;

            if (counts.Count > 0 && counts[0] == 3)
                return PokerHandType.ThreeOfAKind;

            if (counts.Count > 1 && counts[0] == 2 && counts[1] == 2)
                return PokerHandType.TwoPair;

            if (counts.Count > 0 && counts[0] == 2)
                return PokerHandType.Pair;

            return PokerHandType.HighCard;
        }

        private static bool IsFlush(List<Card> hand)
        {
            if (hand.Any(c => c.isWild))
                return true;

            CardSuit firstSuit = hand[0].suit;
            return hand.All(c => c.suit == firstSuit);
        }

        private static bool IsStraight(List<Card> hand)
        {
            var values = hand.Select(c => c.GetNumericValue()).OrderBy(v => v).ToList();

            for (int i = 0; i < values.Count - 1; i++)
            {
                if (values[i + 1] - values[i] != 1)
                {
                    if (values[4] == 14 && values[0] == 2 && values[1] == 3 && values[2] == 4 && values[3] == 5)
                        return true;
                    
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<CardRank, int> GetRankCounts(List<Card> hand)
        {
            var counts = new Dictionary<CardRank, int>();

            foreach (var card in hand)
            {
                if (card.isWild)
                    continue;

                if (!counts.ContainsKey(card.rank))
                    counts[card.rank] = 0;
                
                counts[card.rank]++;
            }

            int wildCount = hand.Count(c => c.isWild);
            if (wildCount > 0 && counts.Count > 0)
            {
                var maxRank = counts.OrderByDescending(kvp => kvp.Value).First().Key;
                counts[maxRank] += wildCount;
            }

            return counts;
        }

        public static string GetHandName(PokerHandType handType)
        {
            return handType switch
            {
                PokerHandType.RoyalFlush => "Royal Flush",
                PokerHandType.StraightFlush => "Straight Flush",
                PokerHandType.FourOfAKind => "Four of a Kind",
                PokerHandType.FullHouse => "Full House",
                PokerHandType.Flush => "Flush",
                PokerHandType.Straight => "Straight",
                PokerHandType.ThreeOfAKind => "Three of a Kind",
                PokerHandType.TwoPair => "Two Pair",
                PokerHandType.Pair => "Pair",
                _ => "High Card"
            };
        }
    }
}
