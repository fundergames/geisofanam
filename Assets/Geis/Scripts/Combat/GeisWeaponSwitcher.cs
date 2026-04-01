// Geis of Anam - Weapon equipping via keys 1-4 (Unarmed, Knife, Sword, Bow).
// Gamepad: D-pad up cycles to the next weapon.
// Uses GeisWeaponDefinition[] as the single source for prefab, combo, and damage.

using UnityEngine;
using UnityEngine.Serialization;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using Geis.SoulRealm;

namespace Geis.Combat
{
    /// <summary>
    /// Switches between weapons using keys 1-4. Slot 0=Unarmed, 1=Knife, 2=Sword, 3=Bow.
    /// Controller D-pad up cycles forward through equipped slots.
    /// Assign GeisWeaponDefinition per slot (prefab + combo + damage).
    /// </summary>
    public class GeisWeaponSwitcher : MonoBehaviour
    {
        [Header("Weapons")]
        [Tooltip("Slots: [0]=Unarmed, [1]=Knife, [2]=Sword, [3]=Bow. Prefab + combo + damage per weapon.")]
        [FormerlySerializedAs("unifiedSlots")]
        [SerializeField]
        private GeisWeaponDefinition[] weaponSlots = new GeisWeaponDefinition[4];

        [Header("Attachment")]
        [Tooltip("Optional: overrides auto-detect for the right-hand weapon socket")]
        [SerializeField]
        private Transform manualAttachmentPoint;

        [Tooltip("Optional: overrides auto-detect for the left-hand weapon socket")]
        [SerializeField]
        private Transform manualLeftAttachmentPoint;

        [Tooltip("Optional: assign Animator manually if on different branch of hierarchy")]
        [SerializeField]
        private Animator manualAnimator;

        [Tooltip("Bone names to search for right-hand weapon attachment")]
        [FormerlySerializedAs("attachmentBoneNames")]
        [SerializeField]
        private string[] rightHandBoneNames = { "weapon_r", "hand_r", "Hand_R", "Weapon" };

        [Tooltip("Bone names to search for left-hand weapon attachment")]
        [SerializeField]
        private string[] leftHandBoneNames = { "weapon_l", "hand_l", "Hand_L", "Weapon_L" };

        [FormerlySerializedAs("useAnimatorRightHandFallback")]
        [SerializeField]
        private bool useAnimatorHandFallback = true;

        private Transform _rightHandAttachment;
        private Transform _leftHandAttachment;
        private GameObject _currentWeaponInstance;
        private int _currentWeaponIndex = -1;
        private Animator _animator;

        /// <summary>
        /// Current weapon index (0-3). -1 if none equipped.
        /// </summary>
        public int CurrentWeaponIndex => _currentWeaponIndex;

        private CombatEntity _combatEntity;

        /// <summary>
        /// Get combo data for the given weapon index. Returns definition.comboData when assigned.
        /// </summary>
        public bool TryGetComboForWeapon(int weaponIndex, out GeisComboData combo)
        {
            combo = null;
            if (weaponSlots != null && weaponIndex >= 0 && weaponIndex < weaponSlots.Length)
            {
                var def = weaponSlots[weaponIndex];
                if (def != null)
                {
                    combo = def.comboData;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the weapon definition at index, or null if out of range / unassigned.
        /// </summary>
        public GeisWeaponDefinition GetWeaponDefinition(int weaponIndex)
        {
            if (weaponSlots == null || weaponIndex < 0 || weaponIndex >= weaponSlots.Length)
                return null;
            return weaponSlots[weaponIndex];
        }

        private void Awake()
        {
            _animator = manualAnimator ?? GetComponent<Animator>() ?? GetComponentInChildren<Animator>() ?? GetComponentInParent<Animator>();
            _combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
            FindAttachmentPoints();
        }

        private void Start()
        {
            if ((_rightHandAttachment == null || _leftHandAttachment == null) && _animator != null)
                FindAttachmentPoints();

            var slotCount = weaponSlots != null ? weaponSlots.Length : 0;
            if (_currentWeaponIndex < 0 && slotCount > 0)
                EquipWeapon(0);
        }

        private void Update()
        {
            if (SoulRealmInteractable.BlockPhysicalInteractions)
                return;

            int slotCount = weaponSlots != null ? Mathf.Min(4, weaponSlots.Length) : 0;
            if (slotCount == 0) return;

            for (int i = 0; i < slotCount; i++)
            {
                if (GetKeyDownForSlot(i))
                {
                    EquipWeapon(i);
                    return;
                }
            }

            if (WasCycleWeaponPressed())
            {
                int cur = _currentWeaponIndex < 0 ? 0 : _currentWeaponIndex;
                int next = (cur + 1) % slotCount;
                EquipWeapon(next);
            }
        }

        private bool GetKeyDownForSlot(int index)
        {
            if (index < 0 || index > 3) return false;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var key = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit1 + index);
                return UnityEngine.InputSystem.Keyboard.current[key].wasPressedThisFrame;
            }
#endif
            return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index));
        }

        /// <summary>
        /// D-pad up (gamepad). Uses same gamepad resolution as <see cref="RogueDeal.Combat.CombatInputReader"/>.
        /// </summary>
        private bool WasCycleWeaponPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad == null && UnityEngine.InputSystem.Gamepad.all.Count > 0)
                gamepad = UnityEngine.InputSystem.Gamepad.all[0];
            if (gamepad != null && gamepad.dpad.up.wasPressedThisFrame)
                return true;
#else
            if (Input.GetKeyDown(KeyCode.JoystickButton3))
                return true;
#endif
            return false;
        }

        private void FindAttachmentPoints()
        {
            if (_animator == null)
            {
                Debug.LogWarning("[GeisWeaponSwitcher] No Animator found.");
                return;
            }

            if (manualAttachmentPoint != null)
                _rightHandAttachment = manualAttachmentPoint;
            else
            {
                _rightHandAttachment = FindHandByBoneNames(_animator, rightHandBoneNames);
                if (_rightHandAttachment == null && useAnimatorHandFallback && _animator.avatar != null)
                    _rightHandAttachment = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (_rightHandAttachment == null)
                    _rightHandAttachment = _animator.transform;
            }

            if (manualLeftAttachmentPoint != null)
                _leftHandAttachment = manualLeftAttachmentPoint;
            else
            {
                _leftHandAttachment = FindHandByBoneNames(_animator, leftHandBoneNames);
                if (_leftHandAttachment == null && useAnimatorHandFallback && _animator.avatar != null)
                    _leftHandAttachment = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
                if (_leftHandAttachment == null)
                    _leftHandAttachment = _rightHandAttachment;
            }
        }

        private static Transform FindHandByBoneNames(Animator animator, string[] boneNames)
        {
            if (boneNames == null)
                return null;
            foreach (var name in boneNames)
            {
                var t = FindTransformRecursive(animator.transform, name);
                if (t != null)
                    return t;
            }

            return null;
        }

        private Transform GetAttachmentParent(GeisWeaponDefinition def)
        {
            if (def != null && def.AttachmentHand == WeaponAttachmentHand.LeftHand)
            {
                if (_leftHandAttachment != null)
                    return _leftHandAttachment;
                Debug.LogWarning(
                    "[GeisWeaponSwitcher] Left-hand attachment not found; using right-hand socket.",
                    this);
            }

            return _rightHandAttachment != null ? _rightHandAttachment : transform;
        }

        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                var found = FindTransformRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Equip weapon at slot index (0=Unarmed, 1=Knife, 2=Sword, 3=Bow).
        /// </summary>
        public void EquipWeapon(int slotIndex)
        {
            if (weaponSlots == null || slotIndex < 0 || slotIndex >= weaponSlots.Length)
                return;

            var def = weaponSlots[slotIndex];
            Transform parent = GetAttachmentParent(def);
            GameObject prefab = null;

            if (def != null)
                prefab = def.weaponPrefab;

            if (_combatEntity != null)
            {
                var data = _combatEntity.GetEntityData();
                if (data != null && def != null)
                    data.equippedWeapon = def.GetWeaponForDamage();
            }

            if (_currentWeaponInstance != null)
            {
                Destroy(_currentWeaponInstance);
                _currentWeaponInstance = null;
            }

            if (prefab != null)
            {
                _currentWeaponInstance = Instantiate(prefab, parent);
                _currentWeaponInstance.transform.localPosition = Vector3.zero;
                _currentWeaponInstance.transform.localRotation = Quaternion.identity;
                _currentWeaponInstance.transform.localScale = Vector3.one;
            }

            _currentWeaponIndex = slotIndex;

            if (_animator != null)
            {
                int hash = Animator.StringToHash("EquippedWeaponIndex");
                foreach (var p in _animator.parameters)
                {
                    if (p.name == "EquippedWeaponIndex")
                    {
                        _animator.SetInteger(hash, slotIndex);
                        break;
                    }
                }
            }
        }
    }
}
