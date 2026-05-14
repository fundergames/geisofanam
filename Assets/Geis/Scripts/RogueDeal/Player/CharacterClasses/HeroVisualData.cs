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

using UnityEngine;

namespace RogueDeal.Player
{
    [CreateAssetMenu(fileName = "Hero Visual Data", menuName = "Funder Games/Geis/Rogue Deal/Character/Hero Visual Data")]
    public class HeroVisualData : ScriptableObject
    {
        public Sprite icon;
        public Sprite fullImage;
        public GameObject characterPrefab;
    }
}
