namespace RogueDeal.Combat.Cards
{
    public static class PokerHandAttackInfo
    {
        public static int GetNumberOfHits(PokerHandType handType)
        {
            return handType switch
            {
                PokerHandType.HighCard => 1,
                PokerHandType.Pair => 2,
                PokerHandType.TwoPair => 2,
                PokerHandType.ThreeOfAKind => 3,
                PokerHandType.Straight => 1,
                PokerHandType.Flush => 3,
                PokerHandType.FullHouse => 4,
                PokerHandType.FourOfAKind => 4,
                PokerHandType.StraightFlush => 5,
                PokerHandType.RoyalFlush => 1,
                _ => 1
            };
        }
    }
}
