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
    /// Default markable target. Add to props or enemy roots; optional VFX on mark.
    /// A procedural ring/cross indicator is shown by default so the mark reads in both soul and physical realm.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulMarkTarget : MonoBehaviour, ISoulMarkable
    {
        [Header("Optional prefab VFX (in addition to procedural indicator)")]
        [SerializeField] private GameObject markVfxPrefab;
        [SerializeField] private Vector3 markVfxOffset;

        [Header("Procedural indicator (both realms)")]
        [SerializeField] private bool showProceduralIndicator = true;
        [SerializeField] private Vector3 proceduralIndicatorLocalOffset = new Vector3(0f, 1.15f, 0f);
        [SerializeField] private float proceduralRingRadius = 0.5f;
        [SerializeField] private Color proceduralColorSoulRealm = new Color(0.35f, 0.95f, 1f, 0.92f);
        [SerializeField] private Color proceduralColorPhysicalRealm = new Color(0.25f, 0.75f, 1f, 0.88f);

        private bool _marked;
        private GameObject _vfx;
        private SoulMarkProceduralIndicator _procedural;

        public Transform MarkTransform => transform;
        public bool IsSoulMarked => _marked;

        public void ApplySoulMark()
        {
            _marked = true;
            if (markVfxPrefab != null && _vfx == null)
                _vfx = Instantiate(markVfxPrefab, transform.position + markVfxOffset, Quaternion.identity, transform);

            if (showProceduralIndicator && _procedural == null)
            {
                _procedural = SoulMarkProceduralIndicator.Create(
                    transform,
                    proceduralIndicatorLocalOffset,
                    proceduralRingRadius,
                    proceduralColorSoulRealm,
                    proceduralColorPhysicalRealm);
            }
        }

        public void ClearSoulMark()
        {
            _marked = false;
            if (_vfx != null)
            {
                Destroy(_vfx);
                _vfx = null;
            }

            if (_procedural != null)
            {
                Destroy(_procedural.gameObject);
                _procedural = null;
            }
        }

        private void OnDestroy()
        {
            if (_vfx != null)
                Destroy(_vfx);
            if (_procedural != null)
                Destroy(_procedural.gameObject);
        }
    }
}
