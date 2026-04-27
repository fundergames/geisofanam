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

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RogueDeal.Player;

namespace RogueDeal.UI
{
    public class CharacterClassDescriptionView : MonoBehaviour
    {
        [SerializeField] private Image characterClassSprite;
        [SerializeField] private TextMeshProUGUI playerName;
        [SerializeField] private TextMeshProUGUI characterClassName;
        [SerializeField] private TextMeshProUGUI description;

        public void UpdateDisplay(HeroData hero)
        {
            if (hero == null) 
            {
                Debug.LogWarning("[CharacterClassDescriptionView] Hero data is null");
                return;
            }

            Debug.Log($"[CharacterClassDescriptionView] UpdateDisplay called for {hero.PlayerName}");

            if (playerName != null)
            {
                playerName.text = hero.PlayerName;
            }

            if (characterClassSprite != null && hero.CharacterClass != null)
            {
                characterClassSprite.sprite = hero.CharacterClass.Icon;
            }

            if (characterClassName != null && hero.CharacterClass != null)
            {
                characterClassName.text = hero.CharacterClass.ClassDisplayName;
            }

            if (description != null && hero.CharacterClass != null)
            {
                description.text = hero.CharacterClass.Description;
            }
        }
    }
}
