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

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Dagger-Flute: object is solid only in its assigned realm and ethereal in the other.
    /// Ethereal props stay visible and raycastable, but their colliders become triggers so the player can walk through them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulPhaseShiftable : MonoBehaviour
    {
        public enum SolidRealmMode
        {
            Physical = 0,
            Soul = 1,
        }

        [SerializeField] private Collider[] phasedColliders;
        [SerializeField] private GameObject ghostVisualRoot;
        [SerializeField] private float ghostScale = 0.85f;

        [Tooltip("Which realm this object starts solid in.")]
        [SerializeField] private SolidRealmMode initialSolidRealm = SolidRealmMode.Physical;

        [Tooltip("Drives realm dissolve pulse + physical semi-transparency; add on the same object or leave empty.")]
        [SerializeField] private SoulPhaseShiftPresentation presentation;

        private bool[] _originalIsTrigger;
        private bool _phaseActive;
        private SolidRealmMode _solidRealm;

        public SolidRealmMode SolidRealm => _solidRealm;

        public bool IsSolidInCurrentRealm
        {
            get
            {
                bool soulActive = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
                return soulActive ? _solidRealm == SolidRealmMode.Soul : _solidRealm == SolidRealmMode.Physical;
            }
        }

        private void Awake()
        {
            if (presentation == null)
                presentation = GetComponent<SoulPhaseShiftPresentation>();

            // Older scene/test data may point ghostVisualRoot back at this same object.
            // Never toggle the owner GameObject itself for ethereal state or the prop disappears entirely.
            if (ghostVisualRoot == gameObject)
                ghostVisualRoot = null;

            if (phasedColliders == null || phasedColliders.Length == 0)
                phasedColliders = GetComponentsInChildren<Collider>();
            _originalIsTrigger = new bool[phasedColliders.Length];
            _solidRealm = initialSolidRealm;
        }

        private void OnEnable()
        {
            SoulRealmManager.SoulRealmStateChanged += OnSoulRealmStateChanged;
            RefreshForCurrentRealm();
        }

        private void OnDisable()
        {
            SoulRealmManager.SoulRealmStateChanged -= OnSoulRealmStateChanged;
            if (_phaseActive)
                ApplyPhased(false);
        }

        private void OnSoulRealmStateChanged()
        {
            RefreshForCurrentRealm();
        }

        public void SetShiftPullProgress01(float progress)
        {
            presentation?.SetPullProgress01(progress);
        }

        public void ClearShiftPullVisual()
        {
            presentation?.SetPullProgress01(0f);
            RefreshForCurrentRealm();
        }

        public void ShiftSolidRealmToCurrentRealm()
        {
            bool soulActive = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            _solidRealm = soulActive ? SolidRealmMode.Soul : SolidRealmMode.Physical;
            RefreshForCurrentRealm();
        }

        public void ResetToInitialSolidRealm()
        {
            _solidRealm = initialSolidRealm;
            RefreshForCurrentRealm();
        }

        private void ApplyPhased(bool phased)
        {
            if (phasedColliders == null)
                return;

            if (phased)
            {
                if (!_phaseActive)
                {
                    _phaseActive = true;
                    for (var i = 0; i < phasedColliders.Length; i++)
                    {
                        var c = phasedColliders[i];
                        if (c == null) continue;
                        if (_originalIsTrigger != null && i < _originalIsTrigger.Length)
                            _originalIsTrigger[i] = c.isTrigger;
                        c.isTrigger = true;
                    }
                }
            }
            else if (_phaseActive)
            {
                _phaseActive = false;
                for (var i = 0; i < phasedColliders.Length; i++)
                {
                    var c = phasedColliders[i];
                    if (c == null) continue;
                    if (_originalIsTrigger != null && i < _originalIsTrigger.Length)
                        c.isTrigger = _originalIsTrigger[i];
                }
            }

            if (ghostVisualRoot != null)
            {
                ghostVisualRoot.SetActive(phased);
                if (phased)
                    ghostVisualRoot.transform.localScale = Vector3.one * ghostScale;
            }
        }

        private void RefreshForCurrentRealm()
        {
            ApplyPhased(!IsSolidInCurrentRealm);
            if (presentation != null)
                presentation.SetSolidified(IsSolidInCurrentRealm);
        }
    }
}
