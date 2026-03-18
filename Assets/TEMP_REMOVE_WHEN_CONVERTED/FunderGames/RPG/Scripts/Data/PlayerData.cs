public class PlayerData
{
    public int coins { get; private set; }

    public PlayerData(int initialCoins)
    {
        coins = initialCoins;
    }

    public void AddCoins(int amount)
    {
        coins += amount;
    }
}
