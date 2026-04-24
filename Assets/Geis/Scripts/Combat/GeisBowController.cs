// Geis of Anam - Bow weapon controller.
// Hold LT (aim) for shoulder camera + crosshair. While aiming with the bow equipped, RT draws the bow and
// releasing RT looses an arrow. Returning from draw falls back to LT aim if still held, otherwise bow idle.
// RT is bound to LightAttack (see GeisControls.inputactions). We *poll* IsPressed() on the LightAttack action each frame
// and detect the rising/falling edge ourselves instead of using started/canceled callbacks — analog triggers can momentarily
// dip below the button release point while held, which otherwise re-fires canceled and spawns phantom arrows.
// Arrows travel toward the camera aim point (raycast hit or max range), not to the nearest enemy.
//
// Animation: Synty AnimationBowCombat (Polygon) — Bow_Draw layer uses A_POLY_BOW_Stand_Shoot_Reload_Neut (see AC_Polygon_Masculine_Geis).
// Naming and optional variants (Lng/Rcv/Cmp): GeisBowSyntyAnimationRefs. For sustained draw while holding RT, enable Loop Time on the Reload clip in the FBX import settings.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Geis.InputSystem;
using Geis.Locomotion;
using Geis.SoulRealm;
using Geis.SoulRealm.WeaponAbilities;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Cooldowns;
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
        private static readonly int BowDrawingAnimatorHash = Animator.StringToHash("BowDrawing");
        private static readonly int BowDrawChargeAnimatorHash = Animator.StringToHash("BowDrawCharge");

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
        [Tooltip("Runs bow shot cooldown from GeisWeaponDefinition combatAction (e.g. Bow_Light_Attack). Auto-found if unset.")]
        [SerializeField] private CombatExecutor _combatExecutor;
        [Tooltip("Optional. Enables soul-mark homing arrows after tagging in Soul Realm.")]
        [SerializeField] private SoulMarkHomingTracker _soulMarkHoming;

        [Header("Arrow Settings")]
        [Tooltip("Arrow prefab — must have a Projectile component")]
        [SerializeField] private GameObject _arrowPrefab;
        [Tooltip("Spawn point for arrows (e.g. bow-tip bone). Leave null to use a default offset above the player.")]
        [SerializeField] private Transform _arrowLaunchPoint;
        [Tooltip("Optional visual arrow nocked on the bow. Direct reference (wins over name lookup). Shown while RT is held, hidden on release.")]
        [SerializeField] private GameObject _drawArrowVisual;
        [Tooltip("Fallback: name of the draw-arrow GameObject to search for under the live player hierarchy (typically a disabled child of the bow's string bone, e.g. Wep_Longbow_String_01/ArrowVisual).")]
        [SerializeField] private string _drawArrowVisualName = "ArrowVisual";
        [Tooltip("Base arrow travel speed (units/sec)")]
        [SerializeField] private float _arrowSpeed = 22f;
        [Tooltip("Maximum range of the aim raycast and arrow flight")]
        [SerializeField] private float _arrowRange = 80f;
        [Tooltip("Layers included in aim raycast. Ignores trigger colliders so lock-on volumes do not shorten the aim point.")]
        [SerializeField] private LayerMask _aimRaycastLayers = ~0;

        [Header("Charged Shot")]
        [Tooltip("How long RT must be held to reach the charged-shot shake / full charge state")]
        [SerializeField] private float _maxChargeTime = 1.5f;
        [Tooltip("Speed multiplier applied at full charge (1 = same speed as quick shot)")]
        [SerializeField] private float _chargedSpeedMultiplier = 1.75f;
        [Tooltip("Damage multiplier applied when the arrow is released after reaching full charge")]
        [SerializeField] private float _chargedDamageMultiplier = 1.5f;

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
        private bool _lightAttackWasPressed;
        private readonly List<Animator> _equippedBowAnimators = new List<Animator>();
        private GameObject _cachedBowAnimatorWeaponRoot;

        private void Awake()
        {
            if (_inputReader == null)       _inputReader       = GetComponent<GeisInputReader>();
            if (_playerController == null)  _playerController  = GetComponent<GeisPlayerAnimationController>();
            if (_weaponSwitcher == null)    _weaponSwitcher    = GetComponent<GeisWeaponSwitcher>();
            if (_combatEntity == null)      _combatEntity      = GetComponent<CombatEntity>();
            if (_combatExecutor == null)    _combatExecutor    = GetComponent<CombatExecutor>();
            if (_soulMarkHoming == null)     _soulMarkHoming    = GetComponent<SoulMarkHomingTracker>()
                ?? GetComponentInParent<SoulMarkHomingTracker>();

            if (_showAimCrosshair)
                EnsureCrosshairBuilt();

            if (_cameraController == null)
                _cameraController = FindFirstObjectByType<GeisCameraController>();

            SetDrawArrowVisible(false);
        }

        private void OnDisable()
        {
            _isCharging = false;
            _lightAttackWasPressed = false;
            ClearBowDrawAnimatorState();
            SetEquippedBowAnimatorState(false, 0f);
            SetCrosshairVisible(false);
            SetDrawArrowVisible(false);
        }

        private void Update()
        {
            if (_playerController == null)
                return;

            PollLightAttackEdges();

            if (_isCharging && IsBowEquipped)
            {
                float charge01 = GetChargeRatio();
                _playerController.SetBowDrawState(true, charge01, IsChargedShotReady);
                SetEquippedBowAnimatorState(true, charge01);
            }
            else
            {
                ClearBowDrawAnimatorState();
                SetEquippedBowAnimatorState(false, 0f);
            }
        }

        /// <summary>
        /// Edge-detects the LightAttack action by polling <c>IsPressed()</c>. One hold = one start + one release,
        /// regardless of analog-trigger jitter around the Input System release threshold.
        /// </summary>
        private void PollLightAttackEdges()
        {
            if (_inputReader == null)
                return;
            InputAction action = _inputReader.LightAttack;
            if (action == null)
                return;

            bool pressed = action.IsPressed();
            if (pressed == _lightAttackWasPressed)
                return;

            _lightAttackWasPressed = pressed;
            if (pressed)
                OnShootStarted();
            else
                OnShootReleased();
        }

        private void ClearBowDrawAnimatorState()
        {
            if (_playerController != null)
                _playerController.SetBowDrawState(false, 0f, false);
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
            if (!IsAimingWithBow) return;
            _isCharging = true;
            _chargeStartTime = Time.time;
            SetDrawArrowVisible(true);
            onChargeStarted?.Invoke();
        }

        private void OnShootReleased()
        {
            if (!_isCharging) return;
            float chargeRatio = GetChargeRatio();
            bool chargedShotReady = chargeRatio >= 0.999f;

            _isCharging = false;
            SetDrawArrowVisible(false);

            if (!IsBowEquipped) return;

            FireArrow(chargeRatio, chargedShotReady ? _chargedDamageMultiplier : 1f);
        }

        private void SetDrawArrowVisible(bool visible)
        {
            GameObject go = ResolveDrawArrowVisual(requireLookup: visible);
            if (go != null && go.activeSelf != visible)
                go.SetActive(visible);
        }

        private void SetEquippedBowAnimatorState(bool drawing, float chargeNormalized01)
        {
            IReadOnlyList<Animator> bowAnimators = ResolveEquippedBowAnimators();
            if (bowAnimators == null || bowAnimators.Count == 0)
                return;

            float clampedCharge = Mathf.Clamp01(chargeNormalized01);
            for (int i = 0; i < bowAnimators.Count; i++)
            {
                Animator animator = bowAnimators[i];
                if (AnimatorHasParameter(animator, "BowDrawing"))
                    animator.SetBool(BowDrawingAnimatorHash, drawing);
                if (AnimatorHasParameter(animator, "BowDrawCharge"))
                    animator.SetFloat(BowDrawChargeAnimatorHash, clampedCharge);
            }
        }

        private IReadOnlyList<Animator> ResolveEquippedBowAnimators()
        {
            GameObject weaponInstance = _weaponSwitcher != null ? _weaponSwitcher.CurrentWeaponInstance : null;
            if (weaponInstance == null)
            {
                _cachedBowAnimatorWeaponRoot = null;
                _equippedBowAnimators.Clear();
                return _equippedBowAnimators;
            }

            if (_cachedBowAnimatorWeaponRoot == weaponInstance && _equippedBowAnimators.Count > 0)
                return _equippedBowAnimators;

            _cachedBowAnimatorWeaponRoot = weaponInstance;
            _equippedBowAnimators.Clear();

            Animator[] animators = weaponInstance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (AnimatorHasParameter(animators[i], "BowDrawing")
                    || AnimatorHasParameter(animators[i], "BowDrawCharge"))
                    _equippedBowAnimators.Add(animators[i]);
            }

            return _equippedBowAnimators;
        }

        private static bool AnimatorHasParameter(Animator animator, string parameterName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == parameterName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Lazily resolves the nocked-arrow visual. Prefers the inspector reference, otherwise searches the player
        /// hierarchy (including inactive children on the runtime-instantiated bow) for a GameObject named
        /// <see cref="_drawArrowVisualName"/>.
        /// </summary>
        private GameObject ResolveDrawArrowVisual(bool requireLookup)
        {
            if (_drawArrowVisual != null)
                return _drawArrowVisual;

            if (!requireLookup || string.IsNullOrEmpty(_drawArrowVisualName))
                return null;

            Transform found = FindChildRecursiveIncludeInactive(transform, _drawArrowVisualName);
            if (found != null)
                _drawArrowVisual = found.gameObject;
            return _drawArrowVisual;
        }

        private static Transform FindChildRecursiveIncludeInactive(Transform parent, string childName)
        {
            if (parent == null) return null;
            if (parent.name == childName) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform hit = FindChildRecursiveIncludeInactive(parent.GetChild(i), childName);
                if (hit != null) return hit;
            }
            return null;
        }

        // ──────────────────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────────────────

        private bool IsBowEquipped =>
            _weaponSwitcher != null && _weaponSwitcher.CurrentWeaponDefinition != null
            && _weaponSwitcher.CurrentWeaponDefinition.IsBowWeapon;

        private bool IsAimingWithBow =>
            _playerController != null && _playerController.IsAiming && IsBowEquipped;

        private bool IsChargedShotReady => GetChargeRatio() >= 0.999f;

        private float GetChargeRatio()
        {
            if (!_isCharging)
                return 0f;

            return _maxChargeTime > 0f
                ? Mathf.Clamp01((Time.time - _chargeStartTime) / _maxChargeTime)
                : 1f;
        }

        private void FireArrow(float chargeRatio, float damageMultiplier)
        {
            if (_arrowPrefab == null)
            {
                Debug.LogWarning("[GeisBowController] No arrow prefab assigned.", this);
                return;
            }

            var bowDefForCd = _weaponSwitcher != null ? _weaponSwitcher.CurrentWeaponDefinition : null;
            if (bowDefForCd == null || !bowDefForCd.IsBowWeapon)
                return;
            CombatAction bowCombatAction = bowDefForCd != null ? bowDefForCd.GetCombatAction() : null;
            if (_combatExecutor != null && bowCombatAction != null)
            {
                ActionCooldownManager cooldowns = _combatExecutor.GetCooldownManager();
                if (cooldowns != null && !cooldowns.IsActionAvailable(bowCombatAction))
                    return;
            }

            Vector3 spawnPos = GetArrowSpawnWorldPosition();

            // Determine world-space aim point by raycasting from the camera forward.
            Vector3 aimPoint = GetCameraAimPoint(out CombatEntity aimHitEntity);
            Vector3 initialShotDirection = aimPoint - spawnPos;
            if (initialShotDirection.sqrMagnitude < 1e-6f)
            {
                Camera cam = GetGameplayCamera();
                initialShotDirection = cam != null ? cam.transform.forward : transform.forward;
            }
            else
                initialShotDirection.Normalize();

            var arrow = Instantiate(_arrowPrefab, spawnPos, Quaternion.identity);
            if (!arrow.activeSelf)
                arrow.SetActive(true);
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
            if (entityData != null && bowDefForCd != null)
                entityData.equippedWeapon = bowDefForCd.GetWeaponForDamage();

            BaseEffect[] effects = ResolveEffects();

            if (_soulMarkHoming != null && _soulMarkHoming.TryConsumeHomingShot(out Transform homingTarget))
            {
                projectile.InitializeSoulMarkHoming(
                    homingTarget,
                    initialShotDirection,
                    speed,
                    effects,
                    entityData,
                    _combatEntity,
                    aimHitEntity,
                    damageMultiplier);
            }
            else
            {
                projectile.InitializeAimPoint(aimPoint, speed, effects, entityData, aimHitEntity, _combatEntity, damageMultiplier);
            }

            if (_combatExecutor != null && bowCombatAction != null)
            {
                ActionCooldownManager cooldowns = _combatExecutor.GetCooldownManager();
                cooldowns?.StartCooldown(bowCombatAction);
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
        /// <summary>
        /// Camera aim raycast. <paramref name="hitEntity"/> is the <see cref="CombatEntity"/> on the struck collider (parent chain), if any.
        /// </summary>
        private Vector3 GetCameraAimPoint(out CombatEntity hitEntity)
        {
            hitEntity = null;
            Camera cam = GetGameplayCamera();
            if (cam == null)
                return transform.position + transform.forward * _arrowRange;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);

            // In soul realm, boss fists often use PhysicalOnly PuzzleRealmVisual — solid hurtboxes are disabled and only
            // soul-realm trigger colliders (e.g. shield sphere) remain. Those require QueryTriggerInteraction.Collide.
            bool soulRealm = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            if (soulRealm)
            {
                RaycastHit[] hits = Physics.RaycastAll(ray, _arrowRange, _aimRaycastLayers, QueryTriggerInteraction.Collide);
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit h in hits)
                {
                    var ce = h.collider.GetComponent<CombatEntity>() ?? h.collider.GetComponentInParent<CombatEntity>();
                    if (ce != null)
                    {
                        // In Soul Realm, the frozen physical body can be in front of the camera and shares the same CombatEntity.
                        // Never allow the aim-ray to select our own physical self as the damage target.
                        if (_combatEntity != null && ce == _combatEntity)
                            continue;
                        hitEntity = ce;
                        return h.point;
                    }
                }

                return ray.origin + ray.direction * _arrowRange;
            }

            // Physical realm: ignore triggers — lock-on / targeting volumes are often triggers in front of the mesh.
            if (Physics.Raycast(ray, out RaycastHit hit, _arrowRange, _aimRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                hitEntity = hit.collider.GetComponent<CombatEntity>() ?? hit.collider.GetComponentInParent<CombatEntity>();
                return hit.point;
            }

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
            var def = _weaponSwitcher != null ? _weaponSwitcher.CurrentWeaponDefinition : null;
            if (def == null || !def.IsBowWeapon)
                return System.Array.Empty<BaseEffect>();
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
