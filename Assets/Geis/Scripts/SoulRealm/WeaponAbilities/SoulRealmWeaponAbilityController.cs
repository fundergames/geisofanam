using Geis.Combat;
using Geis.Locomotion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Routes soul-realm-only weapon ability input to the current weapon's ability assets.
    /// Ability bindings live on <see cref="soulAbilityActions"/> so main <see cref="GeisControls"/> stays unchanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulRealmWeaponAbilityController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GeisWeaponSwitcher weaponSwitcher;

        [Tooltip("Input map SoulRealmWeapon (Ability1 / Ability2). Assign SoulRealmWeaponAbilities asset.")]
        [SerializeField] private InputActionAsset soulAbilityActions;

        [Tooltip("Defaults to this transform when building ability context.")]
        [SerializeField] private Transform abilityOrigin;

        [Tooltip("Used for ability forward and screen-center raycasts. Auto-found if unset.")]
        [SerializeField] private GeisCameraController cameraController;

        private InputActionMap _abilityMap;
        private InputAction _ability1;
        private InputAction _ability2;

        private void Awake()
        {
            if (weaponSwitcher == null)
                weaponSwitcher = GetComponent<GeisWeaponSwitcher>() ?? GetComponentInParent<GeisWeaponSwitcher>();
            if (abilityOrigin == null)
                abilityOrigin = transform;
            if (cameraController == null)
                cameraController = FindFirstObjectByType<GeisCameraController>();
        }

        private void OnEnable()
        {
            CacheActions();
            SoulRealmManager.SoulRealmStateChanged += OnSoulRealmStateChanged;
        }

        private void OnDisable()
        {
            SoulRealmManager.SoulRealmStateChanged -= OnSoulRealmStateChanged;
            if (_abilityMap != null && _abilityMap.enabled)
                _abilityMap.Disable();
        }

        private void OnSoulRealmStateChanged()
        {
            SyncActionMapWithRealm();
        }

        private void Update()
        {
            SyncActionMapWithRealm();
            if (_abilityMap == null || !_abilityMap.enabled)
                return;

            if (_ability1 != null && _ability1.WasPressedThisFrame())
                TryActivateAbility(0);
            if (_ability2 != null && _ability2.WasPressedThisFrame())
                TryActivateAbility(1);
        }

        private void CacheActions()
        {
            if (soulAbilityActions == null || _abilityMap != null)
                return;

            _abilityMap = soulAbilityActions.FindActionMap("SoulRealmWeapon");
            if (_abilityMap == null)
            {
                Debug.LogError(
                    "[SoulRealmWeaponAbilityController] Input asset needs action map named \"SoulRealmWeapon\".",
                    this);
                return;
            }

            _ability1 = _abilityMap.FindAction("Ability1");
            _ability2 = _abilityMap.FindAction("Ability2");
            if (_ability1 == null || _ability2 == null)
                Debug.LogError("[SoulRealmWeaponAbilityController] Map must define Ability1 and Ability2.", this);
        }

        private void SyncActionMapWithRealm()
        {
            CacheActions();
            if (_abilityMap == null)
                return;

            bool want =
                SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;

            if (_abilityMap.enabled != want)
            {
                if (want)
                    _abilityMap.Enable();
                else
                    _abilityMap.Disable();
            }
        }

        private void TryActivateAbility(int slot)
        {
            if (weaponSwitcher == null)
                return;

            int weaponIndex = weaponSwitcher.CurrentWeaponIndex;
            if (weaponIndex < 0)
                return;

            GeisWeaponDefinition def = weaponSwitcher.GetWeaponDefinition(weaponIndex);
            if (def == null)
                return;

            SoulWeaponAbilityAsset ability = slot == 0 ? def.PrimarySoulAbility : def.SecondarySoulAbility;
            if (ability == null)
                return;

            Camera cam = cameraController != null ? cameraController.MainCamera : Camera.main;
            Vector3 forward = cameraController != null
                ? cameraController.GetCameraForwardZeroedYNormalised()
                : (abilityOrigin != null ? abilityOrigin.forward : Vector3.forward);
            Vector3 origin = abilityOrigin != null ? abilityOrigin.position : transform.position;

            var ctx = new SoulWeaponAbilityContext(weaponIndex, def, abilityOrigin, cam, forward, origin);
            ability.Activate(in ctx);
        }
    }
}
