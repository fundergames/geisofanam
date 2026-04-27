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
using Geis.InputSystem;
using Geis.SoulRealm;
using RogueDeal.Combat;
using UnityEngine;

namespace RogueDeal.Boss
{
    /// <summary>
    /// A soul-realm-only shield / target with its own HP pool.
    /// Supports ghost light-attack input when the ghost enters the trigger zone, and
    /// soul-realm projectile damage via <see cref="ISoulRealmShieldProjectileSink"/>.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class SoulShieldTarget : MonoBehaviour, ISoulRealmShieldProjectileSink
    {
        public static event Action<SoulShieldTarget> OnBroken;

        [Header("Interaction")]
        [SerializeField] private float interactionRadius = 2.5f;
        [SerializeField] private float maxHealth = 75f;
        [SerializeField] private float damagePerHit = 25f;

        [Header("Visuals")]
        [Tooltip("Auto-detected from children if empty")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private GameObject interactPromptObject;

        private bool _active;
        private bool _ghostInRange;
        private float _health;

        private SphereCollider _trigger;
        private GeisInputReader _inputReader;

        public bool IsActive => _active;

        private void Awake()
        {
            _trigger = GetComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.radius = interactionRadius;

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Start()
        {
            _inputReader = FindFirstObjectByType<GeisInputReader>();
            ResetShield();
            SetActive(false);
        }

        private void OnEnable()
        {
            if (_inputReader != null)
                _inputReader.onLightAttackPerformed += HandleAttackInput;
        }

        private void OnDisable()
        {
            if (_inputReader != null)
                _inputReader.onLightAttackPerformed -= HandleAttackInput;
        }

        private void Update()
        {
            bool inSoulRealm = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            bool visible = _active && inSoulRealm;
            SetVisible(visible);
            SetPromptVisible(visible && _ghostInRange);
        }

        public void ResetShield()
        {
            _health = Mathf.Max(0.0001f, maxHealth);
        }

        public void SetActive(bool active)
        {
            _active = active;
            _ghostInRange = false;
            SetVisible(false);
            SetPromptVisible(false);
        }

        public bool TryConsumeSoulRealmProjectileDamage(ref float damageAmount, Vector3 hitPosition)
        {
            if (!_active)
                return false;
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                return false;

            if (damageAmount <= 0f)
                damageAmount = damagePerHit;
            if (damageAmount <= 0f)
                return false;

            ApplyDamage(damageAmount);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<SoulGhostMotor>() != null)
                _ghostInRange = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SoulGhostMotor>() != null)
                _ghostInRange = false;
        }

        private void HandleAttackInput()
        {
            if (!_active || !_ghostInRange)
                return;
            if (SoulRealmManager.Instance == null || !SoulRealmManager.Instance.IsSoulRealmActive)
                return;

            ApplyDamage(damagePerHit);
        }

        private void ApplyDamage(float amount)
        {
            if (amount <= 0f)
                return;

            _health -= amount;
            if (_health > 0f)
                return;

            _health = 0f;
            SetActive(false);
            OnBroken?.Invoke(this);
        }

        private void SetVisible(bool visible)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                    r.enabled = visible;
            }

            if (_trigger != null)
                _trigger.enabled = visible;
        }

        private void SetPromptVisible(bool visible)
        {
            if (interactPromptObject != null)
                interactPromptObject.SetActive(visible);
        }
    }
}
