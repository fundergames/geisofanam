using RogueDeal.Player;

namespace RogueDeal.CharacterSelect
{
    public static class CharacterSelectData
    {
        private static HeroData _selectedHero;

        public static HeroData GetSelectedHero()
        {
            return _selectedHero;
        }

        public static void SetSelectedHero(HeroData hero)
        {
            _selectedHero = hero;
        }

        public static void ClearSelection()
        {
            _selectedHero = null;
        }

        public static bool HasSelection()
        {
            return _selectedHero != null;
        }
    }
}
