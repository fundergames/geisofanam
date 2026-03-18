namespace RogueDeal.Player
{
    public class PlayerData
    {
        public int Coins { get; private set; }

        public PlayerData(int initialCoins)
        {
            Coins = initialCoins;
        }

        public void AddCoins(int amount)
        {
            Coins += amount;
        }

        public bool TrySpendCoins(int amount)
        {
            if (Coins >= amount)
            {
                Coins -= amount;
                return true;
            }
            return false;
        }
    }
}
