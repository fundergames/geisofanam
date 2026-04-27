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
    [CreateAssetMenu(menuName = "RogueDeal/Character/Stat Data", fileName = "StatData")]
    public class StatData : ScriptableObject
    {
        [SerializeField] private Sprite icon;
        [SerializeField] private string displayText;
        [SerializeField] private int amount;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private StatType type;
        
        public Sprite Icon => icon;
        public string DisplayText => displayText;
        public int Amount => amount;
        public Color Color => color;
        public StatType Type => type;
    }
}
