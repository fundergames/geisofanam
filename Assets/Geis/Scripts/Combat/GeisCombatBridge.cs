/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

// Geis of Anam - Bridges GeisPlayerAnimationController to RogueDeal combat (damage, hit detection).

using System.Collections;
using UnityEngine;
using Geis.Locomotion;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;

namespace Geis.Combat
{
    /// <summary>
    /// Connects Geis player attacks to RogueDeal combat. Prefers <see cref="WeaponHitbox"/> on the equipped weapon;
    /// falls back to <see cref="SimpleAttackHitDetector"/> when no collider path is available (puzzles, ghost melee).
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    [RequireComponent(typeof(CombatExecutor))]
    [RequireComponent(typeof(SimpleAttackHitDetector))]
    public class GeisCombatBridge : MonoBehaviour
    {
        [Header("Weapon definitions (preferred)")]
        [SerializeField] private GeisWeaponSwitcher _weaponSwitcher;

        [Header("References")]
        [SerializeField] private GeisPlayerAnimationController _geisController;
        [SerializeField] private CombatEventReceiver _combatEventReceiver;

        [Header("Hit detection")]
        [Tooltip("When the equipped weapon has a WeaponHitbox, use collider contact instead of SimpleAttackHitDetector spheres.")]
        [SerializeField] private bool _preferWeaponHitbox = true;

        [Tooltip("Seconds each scheduled hitbox window stays active when driven by combo timings (no animation events).")]
        [SerializeField] private float _scheduledHitboxActiveSeconds = 0.12f;

        [Tooltip("Log when attacks are received (for debugging)")]
        [SerializeField] private bool _debugLog;

        private CombatEntity _combatEntity;
        private CombatExecutor _executor;
        private SimpleAttackHitDetector _hitDetector;
        private Coroutine _hitboxScheduleRoutine;
        private int _hitboxScheduleGeneration;

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>();
            _executor = GetComponent<CombatExecutor>();
            _hitDetector = GetComponent<SimpleAttackHitDetector>();

            if (_geisController == null)
                _geisController = GetComponent<GeisPlayerAnimationController>();
            if (_weaponSwitcher == null)
                _weaponSwitcher = GetComponent<GeisWeaponSwitcher>();
            if (_combatEventReceiver == null)
                _combatEventReceiver = GetComponent<CombatEventReceiver>();
        }

        private void OnEnable()
        {
            if (_hitDetector != null)
                _hitDetector.OverrideMeleeProbeOrigin = GetMeleeProbeOriginForCombat;
            if (_geisController != null)
                _geisController.OnAttackPerformed += HandleAttackPerformed;
        }

        private void OnDisable()
        {
            if (_hitDetector != null)
                _hitDetector.OverrideMeleeProbeOrigin = null;
            if (_geisController != null)
                _geisController.OnAttackPerformed -= HandleAttackPerformed;
            StopHitboxSchedule();
        }

        private (Vector3 origin, Vector3 planarForward) GetMeleeProbeOriginForCombat()
        {
            if (SoulRealmManager.Instance != null &&
                SoulRealmManager.Instance.TryGetGhostMeleeOrigin(out Vector3 p, out Vector3 f))
                return (p, f);

            return (transform.position, transform.forward);
        }

        private void HandleAttackPerformed(int weaponIndex)
        {
            if (_combatEntity == null || _executor == null)
                return;

            CombatAction action = null;
            Weapon weapon = null;
            GeisComboData comboData = null;
            int comboState = _geisController != null ? _geisController.CurrentComboState : 0;

            GeisWeaponDefinition def = _weaponSwitcher != null ? _weaponSwitcher.GetWeaponDefinition(weaponIndex) : null;
            if (def != null)
            {
                comboData = def.comboData;
                action = def.GetCombatAction();
                if (comboData != null)
                    action = comboData.ResolveCombatAction(comboState, action);
                weapon = def.GetWeaponForDamage();
            }

            if (action == null)
            {
                if (_debugLog)
                    Debug.Log("[GeisCombatBridge] No combat action for weaponIndex " + weaponIndex);
                return;
            }

            CombatEntityData entityData = _combatEntity.GetEntityData();
            if (entityData != null)
                entityData.equippedWeapon = weapon;

            _executor.SetCurrentAction(action);

            bool isBow = def != null && def.IsBowWeapon;
            WeaponHitbox weaponHitbox = ResolveActiveWeaponHitbox();

            if (_combatEventReceiver != null && weaponHitbox != null)
                _combatEventReceiver.SetActiveWeaponHitbox(weaponHitbox);

            if (_preferWeaponHitbox && !isBow && weaponHitbox != null)
            {
                if (_debugLog)
                    Debug.Log($"[GeisCombatBridge] WeaponHitbox path action={action.actionName} comboState={comboState}");

                if (comboData != null && comboData.TryGetMultiHitTimesSeconds(comboState, out float[] hitTimes)
                    && hitTimes != null && hitTimes.Length > 0)
                {
                    StartHitboxSchedule(weaponHitbox, hitTimes);
                }

                return;
            }

            if (_hitDetector == null)
                return;

            if (_debugLog)
                Debug.Log($"[GeisCombatBridge] SimpleAttackHitDetector fallback action={action.actionName}");

            if (comboData != null && comboData.TryGetMultiHitTimesSeconds(comboState, out float[] geisTimes) &&
                geisTimes != null && geisTimes.Length > 0)
            {
                _hitDetector.PerformHitCheck(action, geisTimes, weaponIndex);
            }
            else
            {
                _hitDetector.PerformHitCheck(action, null, weaponIndex);
            }
        }

        private WeaponHitbox ResolveActiveWeaponHitbox()
        {
            if (_weaponSwitcher != null && _weaponSwitcher.CurrentWeaponInstance != null)
            {
                WeaponHitbox onWeapon = _weaponSwitcher.CurrentWeaponInstance.GetComponentInChildren<WeaponHitbox>(true);
                if (onWeapon != null)
                    return onWeapon;
            }

            return GetComponentInChildren<WeaponHitbox>(true);
        }

        private void StartHitboxSchedule(WeaponHitbox hitbox, float[] timesFromAttackStartSeconds)
        {
            StopHitboxSchedule();
            _hitboxScheduleGeneration++;
            _hitboxScheduleRoutine = StartCoroutine(HitboxScheduleCoroutine(hitbox, timesFromAttackStartSeconds, _hitboxScheduleGeneration));
        }

        private void StopHitboxSchedule()
        {
            if (_hitboxScheduleRoutine != null)
            {
                StopCoroutine(_hitboxScheduleRoutine);
                _hitboxScheduleRoutine = null;
            }

            WeaponHitbox hb = ResolveActiveWeaponHitbox();
            hb?.Disable();
        }

        private IEnumerator HitboxScheduleCoroutine(WeaponHitbox hitbox, float[] times, int generation)
        {
            float elapsed = 0f;
            for (int i = 0; i < times.Length; i++)
            {
                if (generation != _hitboxScheduleGeneration || hitbox == null)
                    yield break;

                float targetTime = Mathf.Max(0f, times[i]);
                float wait = Mathf.Max(0f, targetTime - elapsed);
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);
                elapsed = Mathf.Max(elapsed, targetTime);

                if (generation != _hitboxScheduleGeneration || hitbox == null)
                    yield break;

                hitbox.Enable();
                yield return new WaitForSeconds(_scheduledHitboxActiveSeconds);
                hitbox.Disable();
            }

            if (generation == _hitboxScheduleGeneration)
                _hitboxScheduleRoutine = null;
        }
    }
}
