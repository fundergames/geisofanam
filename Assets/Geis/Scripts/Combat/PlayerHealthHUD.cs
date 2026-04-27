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

// Screen-space player health bar (bottom-left). Reads CombatEntityData — the same source used by CombatExecutor effects.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;

namespace Geis.Combat
{
    /// <summary>
    /// Displays current / max health for the player. Place on the player root (with <see cref="CombatEntity"/>)
    /// or on a HUD object; optionally leave references empty to build a minimal UI at runtime.
    /// </summary>
    public class PlayerHealthHUD : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Player combat entity. If unset, uses CombatEntity on this GameObject, then tag \"Player\".")]
        [SerializeField] private CombatEntity playerCombatEntity;

        [Header("UI (optional — built at runtime if missing)")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Graphic fillImage;
        [SerializeField] private TextMeshProUGUI healthLabelTmp;
        [SerializeField] private Text healthLabelLegacy;

        [Header("Layout (runtime build)")]
        [SerializeField] private bool buildDefaultHudIfNeeded = true;
        [SerializeField] private Vector2 bottomLeftOffset = new Vector2(24f, 24f);
        [SerializeField] private Vector2 barSize = new Vector2(280f, 22f);
        [SerializeField] private int canvasSortOrder = 90;

        [Header("Colours")]
        [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.92f);
        [SerializeField] private Color fillHighColor = new Color(0.2f, 0.78f, 0.35f, 1f);
        [SerializeField] private Color fillMidColor = new Color(0.95f, 0.75f, 0.15f, 1f);
        [SerializeField] private Color fillLowColor = new Color(0.9f, 0.22f, 0.18f, 1f);
        [SerializeField] [Range(0f, 1f)] private float lowHealthThreshold = 0.25f;
        [SerializeField] [Range(0f, 1f)] private float midHealthThreshold = 0.5f;

        private CombatEntity _resolvedEntity;
        private float _lastCurrent = -1f;
        private float _lastMax = -1f;

        private void Awake()
        {
            ResolvePlayerEntity();
            if (buildDefaultHudIfNeeded && healthSlider == null)
                BuildDefaultHud();
        }

        private void OnEnable()
        {
            ResolvePlayerEntity();
            _lastCurrent = _lastMax = -1f;
            RefreshFromEntityData();
        }

        private void LateUpdate()
        {
            RefreshFromEntityData();
        }

        private void ResolvePlayerEntity()
        {
            if (playerCombatEntity != null)
            {
                _resolvedEntity = playerCombatEntity;
                return;
            }

            _resolvedEntity = GetComponent<CombatEntity>();
            if (_resolvedEntity == null)
            {
                var tagged = GameObject.FindGameObjectWithTag("Player");
                if (tagged != null)
                    _resolvedEntity = tagged.GetComponent<CombatEntity>();
            }
        }

        private void BuildDefaultHud()
        {
            var hudRoot = new GameObject("PlayerHealthHUD_Canvas");
            hudRoot.transform.SetParent(transform, false);
            hudRoot.layer = LayerMask.NameToLayer("UI");

            var canvas = hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = canvasSortOrder;

            var scaler = hudRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            hudRoot.AddComponent<GraphicRaycaster>();

            var canvasRt = hudRoot.GetComponent<RectTransform>();
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;

            var container = new GameObject("HealthBarRoot");
            container.transform.SetParent(hudRoot.transform, false);
            var containerRt = container.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0f, 0f);
            containerRt.anchorMax = new Vector2(0f, 0f);
            containerRt.pivot = new Vector2(0f, 0f);
            containerRt.anchoredPosition = bottomLeftOffset;
            containerRt.sizeDelta = new Vector2(barSize.x, barSize.y + 18f);

            var resources = CreateRuntimeUiResources();
            var sliderObj = DefaultControls.CreateSlider(resources);
            sliderObj.name = "HealthSlider";
            sliderObj.transform.SetParent(container.transform, false);

            var sliderRt = sliderObj.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0f, 0f);
            sliderRt.anchorMax = new Vector2(1f, 1f);
            sliderRt.pivot = new Vector2(0.5f, 0.5f);
            sliderRt.offsetMin = new Vector2(0f, 14f);
            sliderRt.offsetMax = new Vector2(0f, 0f);

            healthSlider = sliderObj.GetComponent<Slider>();
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.wholeNumbers = false;
            healthSlider.interactable = false;
            if (healthSlider.handleRect != null)
                healthSlider.handleRect.gameObject.SetActive(false);

            var bg = sliderObj.transform.Find("Background")?.GetComponent<Image>();
            if (bg != null)
                bg.color = backgroundColor;

            var fillRt = sliderObj.transform.Find("Fill Area/Fill") as RectTransform;
            if (fillRt != null)
            {
                fillImage = fillRt.GetComponent<Graphic>();
                if (fillImage != null)
                    fillImage.color = fillHighColor;
            }

            var labelGo = new GameObject("HealthLabel");
            labelGo.transform.SetParent(container.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0f, 1f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(0f, 16f);

            healthLabelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            healthLabelTmp.fontSize = 14f;
            healthLabelTmp.margin = new Vector4(2f, 0f, 0f, 0f);
            healthLabelTmp.alignment = TextAlignmentOptions.BottomLeft;
            healthLabelTmp.color = Color.white;
            if (TMP_Settings.defaultFontAsset != null)
                healthLabelTmp.font = TMP_Settings.defaultFontAsset;
        }

        private static DefaultControls.Resources CreateRuntimeUiResources()
        {
            var sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            return new DefaultControls.Resources
            {
                standard = sprite,
                background = sprite,
                inputField = sprite,
                knob = sprite
            };
        }

        private void RefreshFromEntityData()
        {
            if (_resolvedEntity == null)
                ResolvePlayerEntity();
            if (_resolvedEntity == null || healthSlider == null)
                return;

            CombatEntityData data = _resolvedEntity.GetEntityData();
            if (data == null)
                return;

            float max = data.maxHealth;
            float cur = data.currentHealth;
            if (Mathf.Approximately(cur, _lastCurrent) && Mathf.Approximately(max, _lastMax))
                return;

            _lastCurrent = cur;
            _lastMax = max;

            float t = max > 0f ? Mathf.Clamp01(cur / max) : 0f;
            healthSlider.value = t;

            if (fillImage != null)
                fillImage.color = ColorForFraction(t);

            string text = $"{Mathf.CeilToInt(cur)} / {Mathf.CeilToInt(max)}";
            if (healthLabelTmp != null)
                healthLabelTmp.text = text;
            if (healthLabelLegacy != null)
                healthLabelLegacy.text = text;
        }

        private Color ColorForFraction(float health01)
        {
            if (health01 <= lowHealthThreshold)
                return fillLowColor;
            if (health01 <= midHealthThreshold)
                return fillMidColor;
            return fillHighColor;
        }
    }
}
