// Geis of Anam - Bow weapon controller.
// Hold LT (aim) for shoulder camera + crosshair. With bow equipped, RT draw/release always looses an arrow (aim optional).
// Arrows travel toward the camera aim point (raycast hit or max range), not to the nearest enemy.
//
// Animation: Synty AnimationBowCombat (Polygon) — Bow_Draw layer uses A_POLY_BOW_Stand_Shoot_Reload_Neut (see AC_Polygon_Masculine_Geis).
// Naming and optional variants (Lng/Rcv/Cmp): GeisBowSyntyAnimationRefs. For sustained draw while holding RT, enable Loop Time on the Reload clip in the FBX import settings.

using UnityEngine;
using UnityEngine.UI;
using Geis.InputSystem;
using Geis.Locomotion;
using Geis.SoulRealm;
using Geis.SoulRealm.WeaponAbilities;
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
        [Tooltip("Gameplay camera for aim ray (falls back to Camera.main). Auto-found if unset.")]
        [SerializeField] private GeisCameraController _cameraController;
        [Tooltip("GeisPlayerAnimationController on the player")]
        [SerializeField] private GeisPlayerAnimationController _playerController;
        [Tooltip("GeisWeaponSwitcher on the player")]
        [SerializeField] private GeisWeaponSwitcher _weaponSwitcher;
        [Tooltip("CombatEntity on the player")]
        [SerializeField] private CombatEntity _combatEntity;
        [Tooltip("Optional. Enables soul-mark homing arrows after tagging in Soul Realm.")]
        [SerializeField] private SoulMarkHomingTracker _soulMarkHoming;

        [Header("Arrow Settings")]
        [Tooltip("Arrow prefab — must have a Projectile component")]
        [SerializeField] private GameObject _arrowPrefab;
        [Tooltip("Spawn point for arrows (e.g. bow-tip bone). Leave null to use a default offset above the player.")]
        [SerializeField] private Transform _arrowLaunchPoint;
        [Tooltip("Base arrow travel speed (units/sec)")]
        [SerializeField] private float _arrowSpeed = 22f;
        [Tooltip("Maximum range of the aim raycast and arrow flight")]
        [SerializeField] private float _arrowRange = 80f;
        [Tooltip("Layers included in aim raycast. Ignores trigger colliders so lock-on volumes do not shorten the aim point.")]
        [SerializeField] private LayerMask _aimRaycastLayers = ~0;

        [Header("Charged Shot")]
        [Tooltip("How long RT must be held to reach full charge")]
        [SerializeField] private float _maxChargeTime = 1.5f;
        [Tooltip("Speed multiplier applied at full charge (1 = same speed as quick shot)")]
        [SerializeField] private float _chargedSpeedMultiplier = 1.75f;

        [Header("Aim UI")]
        [Tooltip("Screen-center crosshair while aiming with the bow (matches camera aim ray).")]
        [SerializeField] private bool _showAimCrosshair = true;
        [SerializeField] private Color _crosshairColor = new Color(1f, 1f, 1f, 0.75f);
        [Tooltip("Half-length of each crosshair arm in UI pixels (reference resolution).")]
        [SerializeField] private float _crosshairArmHalfLength = 10f;
        [SerializeField] private float _crosshairThickness = 2f;

        private GameObject _crosshairRoot;
        private static Sprite _crosshairSprite;

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
            if (_soulMarkHoming == null)     _soulMarkHoming    = GetComponent<SoulMarkHomingTracker>()
                ?? GetComponentInParent<SoulMarkHomingTracker>();

            if (_showAimCrosshair)
                EnsureCrosshairBuilt();

            if (_cameraController == null)
                _cameraController = FindFirstObjectByType<GeisCameraController>();
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
            ClearBowDrawAnimatorState();
            SetCrosshairVisible(false);
        }

        private void Update()
        {
            if (_playerController == null)
                return;

            if (_isCharging && IsBowEquipped)
            {
                float charge01 = _maxChargeTime > 0f
                    ? Mathf.Clamp01((Time.time - _chargeStartTime) / _maxChargeTime)
                    : 1f;
                _playerController.SetBowDrawState(true, charge01);
            }
            else
                ClearBowDrawAnimatorState();
        }

        private void ClearBowDrawAnimatorState()
        {
            if (_playerController != null)
                _playerController.SetBowDrawState(false, 0f);
        }

        private void LateUpdate()
        {
            if (!_showAimCrosshair || _crosshairRoot == null)
                return;
            SetCrosshairVisible(IsAimingWithBow);
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Input handlers
        // ──────────────────────────────────────────────────────────────────────────

        private void OnShootStarted()
        {
            if (!IsBowEquipped) return;
            _isCharging = true;
            _chargeStartTime = Time.time;
            onChargeStarted?.Invoke();
        }

        private void OnShootReleased()
        {
            if (!_isCharging) return;
            _isCharging = false;

            if (!IsBowEquipped) return;

            float chargeRatio = Mathf.Clamp01((Time.time - _chargeStartTime) / _maxChargeTime);
            FireArrow(chargeRatio);
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private bool IsBowEquipped =>
            _weaponSwitcher != null && _weaponSwitcher.CurrentWeaponIndex == BowSlotIndex;

        private bool IsAimingWithBow =>
            _playerController != null && _playerController.IsAiming && IsBowEquipped;

        private void FireArrow(float chargeRatio)
        {
            if (_arrowPrefab == null)
            {
                Debug.LogWarning("[GeisBowController] No arrow prefab assigned.", this);
                return;
            }

            Vector3 spawnPos = GetArrowSpawnWorldPosition();

            // Determine world-space aim point by raycasting from the camera forward.
            Vector3 aimPoint = GetCameraAimPoint();
            Vector3 initialShotDirection = aimPoint - spawnPos;
            if (initialShotDirection.sqrMagnitude < 1e-6f)
            {
                Camera cam = GetGameplayCamera();
                initialShotDirection = cam != null ? cam.transform.forward : transform.forward;
            }
            else
                initialShotDirection.Normalize();

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

            if (_soulMarkHoming != null && _soulMarkHoming.TryConsumeHomingShot(out Transform homingTarget))
            {
                projectile.InitializeSoulMarkHoming(homingTarget, initialShotDirection, speed, effects, entityData);
            }
            else
            {
                projectile.InitializeAimPoint(aimPoint, speed, effects, entityData);
            }

            onArrowFired?.Invoke(chargeRatio);
        }

        private Vector3 GetArrowSpawnWorldPosition()
        {
            if (SoulRealmManager.Instance != null &&
                SoulRealmManager.Instance.TryGetGhostBowProjectileSpawnWorldPosition(out Vector3 ghostSpawn))
                return ghostSpawn;

            if (_arrowLaunchPoint != null)
                return _arrowLaunchPoint.position;

            return transform.position + Vector3.up * 1.5f;
        }

        /// <summary>
        /// Raycast from the camera forward and return the first hit point,
        /// or the point at max range if nothing is hit.
        /// </summary>
        private Vector3 GetCameraAimPoint()
        {
            Camera cam = GetGameplayCamera();
            if (cam == null)
                return transform.position + transform.forward * _arrowRange;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            // Ignore triggers: lock-on / targeting volumes are often triggers in front of the mesh; we want the solid body hit.
            if (Physics.Raycast(ray, out RaycastHit hit, _arrowRange, _aimRaycastLayers, QueryTriggerInteraction.Ignore))
                return hit.point;

            return ray.origin + ray.direction * _arrowRange;
        }

        private Camera GetGameplayCamera()
        {
            if (_cameraController != null && _cameraController.MainCamera != null)
                return _cameraController.MainCamera;
            return Camera.main;
        }

        private BaseEffect[] ResolveEffects()
        {
            var def    = _weaponSwitcher != null ? _weaponSwitcher.GetWeaponDefinition(BowSlotIndex) : null;
            var action = def?.GetCombatAction();
            return action?.effects ?? System.Array.Empty<BaseEffect>();
        }

        private void EnsureCrosshairBuilt()
        {
            if (_crosshairRoot != null)
                return;

            _crosshairRoot = new GameObject("BowAimCrosshair");
            _crosshairRoot.transform.SetParent(transform, false);

            var canvas = _crosshairRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            canvas.overrideSorting = true;

            var scaler = _crosshairRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _crosshairRoot.AddComponent<GraphicRaycaster>();

            var canvasRt = _crosshairRoot.GetComponent<RectTransform>();
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;

            var center = new GameObject("CrosshairCenter");
            center.transform.SetParent(_crosshairRoot.transform, false);
            var centerRt = center.AddComponent<RectTransform>();
            centerRt.anchorMin = centerRt.anchorMax = new Vector2(0.5f, 0.5f);
            centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.anchoredPosition = Vector2.zero;
            centerRt.sizeDelta = Vector2.zero;

            CreateCrosshairArm(center.transform, true);
            CreateCrosshairArm(center.transform, false);

            SetCrosshairVisible(false);
        }

        private void CreateCrosshairArm(Transform parent, bool horizontal)
        {
            var go = new GameObject(horizontal ? "CrosshairH" : "CrosshairV");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float half = _crosshairArmHalfLength;
            float t = _crosshairThickness;
            rt.sizeDelta = horizontal ? new Vector2(half * 2f, t) : new Vector2(t, half * 2f);

            var img = go.AddComponent<Image>();
            img.sprite = GetOrCreateCrosshairSprite();
            img.color = _crosshairColor;
            img.raycastTarget = false;
        }

        private static Sprite GetOrCreateCrosshairSprite()
        {
            if (_crosshairSprite != null)
                return _crosshairSprite;
            var tex = Texture2D.whiteTexture;
            _crosshairSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _crosshairSprite;
        }

        private void SetCrosshairVisible(bool visible)
        {
            if (_crosshairRoot != null && _crosshairRoot.activeSelf != visible)
                _crosshairRoot.SetActive(visible);
        }
    }
}
