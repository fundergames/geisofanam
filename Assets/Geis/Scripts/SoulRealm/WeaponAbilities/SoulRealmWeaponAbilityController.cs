using Geis.Combat;
using Geis.Locomotion;
using Geis.SoulRealm;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Routes weapon ability input (separate <see cref="soulAbilityActions"/> map) while Soul Realm is active,
    /// and for physical-realm-only abilities when Soul Realm is off.
    /// Does <b>not</b> swap <see cref="GeisControls"/> / <see cref="Geis.InputSystem.GeisInputReader"/> maps — it
    /// enables the <c>SoulRealmWeapon</c> action map on the assigned asset (typically <see cref="GeisControls"/>) in parallel when abilities should be available.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulRealmWeaponAbilityController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GeisWeaponSwitcher weaponSwitcher;

        [Tooltip("Input map SoulRealmWeapon (Ability1 / Ability2). Use GeisControls (contains that map) or any asset with a SoulRealmWeapon map.")]
        [SerializeField] private InputActionAsset soulAbilityActions;

        [Tooltip("Fallback owner/origin when no SoulRealmManager in scene. Otherwise ghost vs body is resolved on the manager.")]
        [SerializeField] private Transform abilityOrigin;

        [Tooltip("Used for ability forward and screen-center raycasts. Auto-found if unset.")]
        [SerializeField] private GeisCameraController cameraController;

        [Tooltip("On-screen + console messages for ability use / blocked reasons. Added at runtime if missing.")]
        [SerializeField] private SoulRealmAbilityFeedback feedback;

        private InputActionMap _abilityMap;
        private InputAction _ability1;
        private InputAction _ability2;

        private float _nextAbilityMapHintTime;

        private void Awake()
        {
            if (weaponSwitcher == null)
                weaponSwitcher = GetComponent<GeisWeaponSwitcher>() ?? GetComponentInParent<GeisWeaponSwitcher>();
            if (abilityOrigin == null)
                abilityOrigin = transform;
            if (cameraController == null)
                cameraController = FindFirstObjectByType<GeisCameraController>();
            if (feedback == null)
                feedback = GetComponent<SoulRealmAbilityFeedback>();
            if (feedback == null)
                feedback = gameObject.AddComponent<SoulRealmAbilityFeedback>();

            // Runtime clone so this map is not shared with other systems and Enable/Disable state is isolated.
            if (soulAbilityActions != null)
            {
                soulAbilityActions = Instantiate(soulAbilityActions);
                _abilityMap = null;
                _ability1 = null;
                _ability2 = null;
            }
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

            PollAbilityButtons(out bool a1, out bool a2);

            if (_abilityMap == null || !_abilityMap.enabled)
            {
                if ((a1 || a2) && feedback != null && Time.unscaledTime >= _nextAbilityMapHintTime)
                {
                    _nextAbilityMapHintTime = Time.unscaledTime + 0.65f;
                    feedback.ShowBlocked(DescribeWhyAbilityMapIsOff());
                }

                return;
            }

            if (a1)
                TryActivateAbility(0);
            if (a2)
                TryActivateAbility(1);
        }

        private void PollAbilityButtons(out bool a1, out bool a2)
        {
            a1 = _ability1 != null && _ability1.WasPressedThisFrame();
            a2 = _ability2 != null && _ability2.WasPressedThisFrame();
            if (!a1 || !a2)
            {
                var gp = ResolveGamepad();
                if (gp != null)
                {
                    if (!a1 && gp.buttonNorth.wasPressedThisFrame)
                        a1 = true;
                    if (!a2 && gp.rightShoulder.wasPressedThisFrame)
                        a2 = true;
                }

                var kb = Keyboard.current;
                if (kb != null)
                {
                    if (!a1 && kb.qKey.wasPressedThisFrame)
                        a1 = true;
                    if (!a2 && kb.fKey.wasPressedThisFrame)
                        a2 = true;
                }
            }
        }

        private string DescribeWhyAbilityMapIsOff()
        {
            if (soulAbilityActions == null)
                return "Soul abilities: assign an Input Actions asset with map SoulRealmWeapon (e.g. GeisControls).";

            if (weaponSwitcher == null)
                return "Soul abilities: no GeisWeaponSwitcher.";

            if (weaponSwitcher.CurrentWeaponIndex < 0)
                return "Soul abilities: equip a weapon (keys 1–4 / D-pad up).";

            GeisWeaponDefinition def = weaponSwitcher.GetWeaponDefinition(weaponSwitcher.CurrentWeaponIndex);
            if (def == null)
                return "Soul abilities: weapon definition missing for current slot.";

            if (def.PrimarySoulAbility == null && def.SecondarySoulAbility == null)
                return "Soul abilities: this weapon has no abilities assigned (Weapon Definition).";

            bool inSoul = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            if (!inSoul)
                return "Soul abilities: enter Soul Realm (Tab / LB hold), or use physical-only abilities outside.";

            return "Soul abilities: input map should be on — check console for errors.";
        }

        private static Gamepad ResolveGamepad()
        {
            var gp = Gamepad.current;
            if (gp == null && Gamepad.all.Count > 0)
                gp = Gamepad.all[0];
            return gp;
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

            bool want = ShouldEnableAbilityInputMap();

            if (_abilityMap.enabled != want)
            {
                if (want)
                    _abilityMap.Enable();
                else
                    _abilityMap.Disable();
            }
        }

        private bool ShouldEnableAbilityInputMap()
        {
            if (weaponSwitcher == null)
                return false;

            int idx = weaponSwitcher.CurrentWeaponIndex;
            if (idx < 0)
                return false;

            GeisWeaponDefinition def = weaponSwitcher.GetWeaponDefinition(idx);
            if (def == null)
                return false;

            bool inSoul = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;

            bool hasPhysical =
                AbilityAllowedInRealm(def.PrimarySoulAbility, false)
                || AbilityAllowedInRealm(def.SecondarySoulAbility, false);

            if (inSoul)
            {
                // Keep the ability map enabled whenever this weapon has any ability assets, so Q/F and D-pad
                // stay live in Soul Realm. Per-realm rules are enforced in TryActivateAbility.
                return def.PrimarySoulAbility != null || def.SecondarySoulAbility != null;
            }

            return hasPhysical;
        }

        private static bool AbilityAllowedInRealm(SoulWeaponAbilityAsset ability, bool soulRealm)
        {
            if (ability == null)
                return false;
            return soulRealm ? ability.AllowActivationInSoulRealm : ability.AllowActivationInPhysicalRealm;
        }

        private void TryActivateAbility(int slot)
        {
            string slotLabel = slot == 0 ? "Ability 1" : "Ability 2";

            if (weaponSwitcher == null)
            {
                feedback?.ShowBlocked($"{slotLabel}: missing weapon switcher reference.");
                return;
            }

            int weaponIndex = weaponSwitcher.CurrentWeaponIndex;
            if (weaponIndex < 0)
            {
                feedback?.ShowBlocked($"{slotLabel}: no weapon equipped (press 1–4 or D-pad up).");
                return;
            }

            GeisWeaponDefinition def = weaponSwitcher.GetWeaponDefinition(weaponIndex);
            if (def == null)
            {
                feedback?.ShowBlocked($"{slotLabel}: weapon slot {weaponIndex} has no definition.");
                return;
            }

            SoulWeaponAbilityAsset ability = slot == 0 ? def.PrimarySoulAbility : def.SecondarySoulAbility;
            if (ability == null)
            {
                feedback?.ShowBlocked($"{slotLabel}: no ability assigned on this weapon (check Weapon Definition).");
                return;
            }

            bool inSoul = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            if (inSoul)
            {
                if (!ability.AllowActivationInSoulRealm)
                {
                    feedback?.ShowBlocked(
                        $"{slotLabel}: '{ability.AbilityDisplayName}' is not usable in Soul Realm.");
                    return;
                }
            }
            else
            {
                if (!ability.AllowActivationInPhysicalRealm)
                {
                    feedback?.ShowBlocked(
                        $"{slotLabel}: '{ability.AbilityDisplayName}' only works in Soul Realm.");
                    return;
                }
            }

            Transform ownerTransform;
            Vector3 originWorld;
            var mgr = SoulRealmManager.Instance;
            if (mgr != null)
                mgr.GetAbilityContextTransforms(out ownerTransform, out originWorld);
            else
            {
                ownerTransform = abilityOrigin != null ? abilityOrigin : transform;
                originWorld = ownerTransform.position;
            }

            Camera cam = cameraController != null ? cameraController.MainCamera : Camera.main;
            Vector3 forward = cameraController != null
                ? cameraController.GetCameraForwardZeroedYNormalised()
                : GetFlattenedForward(ownerTransform);

            var ctx = new SoulWeaponAbilityContext(weaponIndex, def, ownerTransform, cam, forward, originWorld);
            ability.Activate(in ctx);
            feedback?.ShowActivated(slot, ability.AbilityDisplayName, originWorld, forward);
        }

        private static Vector3 GetFlattenedForward(Transform t)
        {
            if (t == null)
                return Vector3.forward;
            Vector3 f = t.forward;
            f.y = 0f;
            return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        }
    }
}
