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

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RogueDeal.UI
{
    [RequireComponent(typeof(Button)), Serializable]
    public class ClassButton : MonoBehaviour
    {
        [SerializeField] private GameObject selectedButton;
        [SerializeField] private GameObject unselectedButton;
        [SerializeField] private Image selectedIcon;
        [SerializeField] private Image unselectedIcon;
        [SerializeField] private TextMeshProUGUI selectedText;
        [SerializeField] private TextMeshProUGUI unselectedText;
        public Button button;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        public void UpdateDisplay(Sprite icon, string select, string unselect)
        {
            if (selectedIcon != null)
            {
                selectedIcon.sprite = icon;
            }

            if (unselectedIcon != null)
            {
                unselectedIcon.sprite = icon;
            }

            if (selectedText != null)
            {
                selectedText.text = select;
            }

            if (unselectedText != null)
            {
                unselectedText.text = unselect;
            }
        }
        
        public void SetSelected(bool isSelected)
        {
            if (selectedButton != null)
            {
                selectedButton.SetActive(isSelected);
            }

            if (unselectedButton != null)
            {
                unselectedButton.SetActive(!isSelected);
            }
        }
    }
}
