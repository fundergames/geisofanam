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

namespace RogueDeal.UI
{
    [RequireComponent(typeof(Slider))]
    public class StatsSlider : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI displayText;
        [SerializeField] private Slider slider;
        [SerializeField] private Image sliderFillImage;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }
        
        public void UpdateView(string displayName, Sprite statIcon, float statValue, Color statColor)
        {
            if (displayText != null)
            {
                displayText.text = displayName;
                displayText.color = statColor;
            }

            if (icon != null)
            {
                icon.sprite = statIcon;
                icon.color = statColor;
            }

            if (slider != null)
            {
                slider.value = statValue / 100f;
            }

            if (sliderFillImage != null)
            {
                sliderFillImage.color = statColor;
            }
        }
    }
}
