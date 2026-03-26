// Geis of Anam - Bow weapon controller.
// Hold LT (aim) to enter over-the-shoulder camera mode.
// Tap RT to fire a quick arrow; hold RT then release to fire a charged arrow (faster, more force).
// Arrows travel toward the camera aim point (raycast hit or max range), not to the nearest enemy.

using UnityEngine;
using Geis.InputSystem;
using Geis.Locomotion;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Presentation;

namespace Geis.Combat
{
    /// <summary>
    /// Handles ranged bow attacks. Attach to the same player GameObject as
    /// GeisWeaponSwitcher, GeisPlayerAnimationController, and CombatEntity.
    /// Assign an arrow prefab (must have a Projectile component).
    /// </summary>
    public class GeisBowController : MonoBehaviour
    {
        private const int BowSlotIndex = 3;

        [Header("References")]
        [Tooltip("GeisInputReader on the player")]
        [SerializeField] private GeisInputReader _inputReader;
        [Tooltip("GeisPlayerAnimationController on the player")]
        [SerializeField] private GeisPlayerAnimationController _playerController;
        [Tooltip("GeisWeaponSwitcher on the player")]
        [SerializeField] private GeisWeaponSwitcher _weaponSwitcher;
        [Tooltip("CombatEntity on the player")]
        [SerializeField] private CombatEntity _combatEntity;

        [Header("Arrow Settings")]
        [Tooltip("Arrow prefab — must have a Projectile component")]
        [SerializeField] private GameObject _arrowPrefab;
        [Tooltip("Spawn point for arrows (e.g. bow-tip bone). Leave null to use a default offset above the player.")]
        [SerializeField] private Transform _arrowLaunchPoint;
        [Tooltip("Base arrow travel speed (units/sec)")]
        [SerializeField] private float _arrowSpeed = 22f;
        [Tooltip("Maximum range of the aim raycast and arrow flight")]
        [SerializeField] private float _arrowRange = 80f;

        [Header("Charged Shot")]
        [Tooltip("How long RT must be held to reach full charge")]
        [SerializeField] private float _maxChargeTime = 1.5f;
        [Tooltip("Speed multiplier applied at full charge (1 = same speed as quick shot)")]
        [SerializeField] private float _chargedSpeedMultiplier = 1.75f;

        /// <summary>Fired when a charge begins. Useful for hooking up draw animation or UI.</summary>
        public System.Action onChargeStarted;
        /// <summary>Fired when an arrow is released. Arg is charge ratio 0–1.</summary>
        public System.Action<float> onArrowFired;

        private bool _isCharging;
        private float _chargeStartTime;

        private void Awake()
        {
            if (_inputReader == null)       _inputReader       = GetComponent<GeisInputReader>();
            if (_playerController == null)  _playerController  = GetComponent<GeisPlayerAnimationController>();
            if (_weaponSwitcher == null)    _weaponSwitcher    = GetComponent<GeisWeaponSwitcher>();
            if (_combatEntity == null)      _combatEntity      = GetComponent<CombatEntity>();
        }

        private void OnEnable()
        {
            if (_inputReader == null) return;
            _inputReader.onHeavyAttackStarted  += OnShootStarted;
            _inputReader.onHeavyAttackReleased += OnShootReleased;
        }

        private void OnDisable()
        {
            if (_inputReader == null) return;
            _inputReader.onHeavyAttackStarted  -= OnShootStarted;
            _inputReader.onHeavyAttackReleased -= OnShootReleased;
            _isCharging = false;
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Input handlers
        // ──────────────────────────────────────────────────────────────────────────

        private void OnShootStarted()
        {
            if (!IsAimingWithBow) return;
            _isCharging = true;
            _chargeStartTime = Time.time;
            onChargeStarted?.Invoke();
        }

        private void OnShootReleased()
        {
            if (!_isCharging) return;
            _isCharging = false;

            // Only fire if the bow is still equipped and the player is still aiming.
            if (!IsAimingWithBow) return;

            float chargeRatio = Mathf.Clamp01((Time.time - _chargeStartTime) / _maxChargeTime);
            FireArrow(chargeRatio);
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private bool IsAimingWithBow =>
            _playerController != null  && _playerController.IsAiming &&
            _weaponSwitcher   != null  && _weaponSwitcher.CurrentWeaponIndex == BowSlotIndex;

        private void FireArrow(float chargeRatio)
        {
            if (_arrowPrefab == null)
            {
                Debug.LogWarning("[GeisBowController] No arrow prefab assigned.", this);
                return;
            }

            Vector3 spawnPos = _arrowLaunchPoint != null
                ? _arrowLaunchPoint.position
                : transform.position + Vector3.up * 1.5f;

            // Determine world-space aim point by raycasting from the camera forward.
            Vector3 aimPoint = GetCameraAimPoint();

            var arrow = Instantiate(_arrowPrefab, spawnPos, Quaternion.identity);
            var projectile = arrow.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogWarning("[GeisBowController] Arrow prefab is missing a Projectile component.", this);
                Destroy(arrow);
                return;
            }

            // Quick-tap = base speed; full charge = chargedSpeedMultiplier × base speed.
            float speed = Mathf.Lerp(_arrowSpeed, _arrowSpeed * _chargedSpeedMultiplier, chargeRatio);

            CombatEntityData entityData = _combatEntity != null ? _combatEntity.GetEntityData() : null;
            BaseEffect[] effects = ResolveEffects();

            projectile.InitializeAimPoint(aimPoint, speed, effects, entityData);
            onArrowFired?.Invoke(chargeRatio);
        }

        /// <summary>
        /// Raycast from the camera forward and return the first hit point,
        /// or the point at max range if nothing is hit.
        /// </summary>
        private Vector3 GetCameraAimPoint()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return transform.position + transform.forward * _arrowRange;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _arrowRange))
                return hit.point;

            return ray.origin + ray.direction * _arrowRange;
        }

        private BaseEffect[] ResolveEffects()
        {
            var def    = _weaponSwitcher != null ? _weaponSwitcher.GetWeaponDefinition(BowSlotIndex) : null;
            var action = def?.GetCombatAction();
            return action?.effects ?? System.Array.Empty<BaseEffect>();
        }
    }
}
