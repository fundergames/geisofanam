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
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// A physical-realm-only shield / target with its own HP pool.
    /// Uses a CombatEntity as a hit target, but immediately restores its entity HP so it never "dies".
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public sealed class PhysicalShieldTarget : MonoBehaviour, IPhysicalWeaponHitGate
    {
        public static event Action<PhysicalShieldTarget> OnBroken;

        [Header("Tuning")]
        [SerializeField] private float maxHealth = 50f;

        [Header("Visuals")]
        [Tooltip("Optional visuals toggled with Active state. If empty, this GameObject is toggled instead.")]
        [SerializeField] private GameObject visualsRoot;

        private CombatEntity _combatEntity;
        private CombatEntityData _entityData;
        private float _health;
        private bool _active;

        public bool IsActive => _active;

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>();
        }

        private void Start()
        {
            _combatEntity.InitializeStatsWithoutHeroData(99999f, 0f, 0f);
            _entityData = _combatEntity.GetEntityData();
            ResetShield();
            SetActive(false);
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += HandleDamageApplied;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
        }

        public bool AllowsPhysicalWeaponHits()
        {
            if (!_active)
                return false;
            // Physical-only: treat hits during soul realm as immune.
            return SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive;
        }

        public void ResetShield()
        {
            _health = Mathf.Max(0.0001f, maxHealth);
            if (_entityData != null)
                _entityData.currentHealth = _entityData.maxHealth;
        }

        public void SetActive(bool active)
        {
            _active = active;

            if (visualsRoot != null)
                visualsRoot.SetActive(active);
            else
                gameObject.SetActive(active);
        }

        private void HandleDamageApplied(CombatEventData data)
        {
            if (data.target != _combatEntity) return;
            if (data.wasImmune) return;
            if (data.skipEntityDamageInterceptors) return;

            // Always restore entity HP; this is a hit target, not a true combatant.
            _entityData?.Heal(data.damageAmount);

            if (!AllowsPhysicalWeaponHits())
                return;

            _health -= data.damageAmount;
            if (_health > 0f)
                return;

            _health = 0f;
            SetActive(false);
            OnBroken?.Invoke(this);
        }
    }
}
