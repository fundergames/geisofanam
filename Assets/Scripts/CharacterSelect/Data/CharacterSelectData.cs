/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

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
