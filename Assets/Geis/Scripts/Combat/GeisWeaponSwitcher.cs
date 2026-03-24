// Geis of Anam - Weapon equipping via keys 1-4 (Unarmed, Knife, Sword, Bow).
// Supports unified GeisWeaponDefinition (single source) or legacy GeisWeaponSlot.

using UnityEngine;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;

namespace Geis.Combat
{
    /// <summary>
    /// Defines a weapon slot: prefab to instantiate (null for unarmed) and display name.
    /// Legacy: use GeisWeaponDefinition for unified mode.
    /// </summary>
    [System.Serializable]
    public class GeisWeaponSlot
    {
        [Tooltip("Prefab to attach to hand (null = unarmed)")]
        public GameObject weaponPrefab;
        [Tooltip("Display name for debug")]
        public string displayName;
    }

    /// <summary>
    /// Switches between weapons using keys 1-4. Slot 0=Unarmed, 1=Knife, 2=Sword, 3=Bow.
    /// Unified mode: use GeisWeaponDefinition[] - single source for prefab, combo, damage.
    /// Legacy: use GeisWeaponSlot[] for visuals only.
    /// </summary>
    public class GeisWeaponSwitcher : MonoBehaviour
    {
        [Header("Mode")]
        [Tooltip("When true, use unified weapon definitions (prefab + combo + damage). When false, use legacy slots.")]
        [SerializeField] private bool useUnifiedWeapons = false;

        [Header("Unified Weapons (single source of truth)")]
        [Tooltip("Slots: [0]=Unarmed, [1]=Knife, [2]=Sword, [3]=Bow. Replaces separate slot/combo/action arrays.")]
        [SerializeField] private GeisWeaponDefinition[] unifiedSlots = new GeisWeaponDefinition[4];

        [Header("Legacy Slots (visuals only)")]
        [Tooltip("When useUnifiedWeapons=false. Slots: [0]=Unarmed, [1]=Knife, [2]=Sword, [3]=Bow")]
        [SerializeField]
        private GeisWeaponSlot[] slots = new GeisWeaponSlot[4];

        [Header("Attachment")]
        [Tooltip("Optional: assign manually if auto-detect fails")]
        [SerializeField]
        private Transform manualAttachmentPoint;

        [Tooltip("Optional: assign Animator manually if on different branch of hierarchy")]
        [SerializeField]
        private Animator manualAnimator;

        [Tooltip("Bone names to search for weapon attachment")]
        [SerializeField]
        private string[] attachmentBoneNames = { "weapon_r", "hand_r", "Hand_R", "Weapon" };

        [SerializeField]
        private bool useAnimatorRightHandFallback = true;

        private Transform _attachmentPoint;
        private GameObject _currentWeaponInstance;
        private int _currentWeaponIndex = -1;
        private Animator _animator;

        /// <summary>
        /// Current weapon index (0-3). -1 if none equipped.
        /// </summary>
        public int CurrentWeaponIndex => _currentWeaponIndex;

        private CombatEntity _combatEntity;

        /// <summary>
        /// Get combo data for the given weapon index. When using unified mode, returns definition.comboData.
        /// </summary>
        public bool TryGetComboForWeapon(int weaponIndex, out GeisComboData combo)
        {
            combo = null;
            if (useUnifiedWeapons && unifiedSlots != null && weaponIndex >= 0 && weaponIndex < unifiedSlots.Length)
            {
                var def = unifiedSlots[weaponIndex];
                if (def != null)
                {
                    combo = def.comboData;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the unified weapon definition at index. Null if legacy mode or out of range.
        /// </summary>
        public GeisWeaponDefinition GetWeaponDefinition(int weaponIndex)
        {
            if (!useUnifiedWeapons || unifiedSlots == null || weaponIndex < 0 || weaponIndex >= unifiedSlots.Length)
                return null;
            return unifiedSlots[weaponIndex];
        }

        private void Awake()
        {
            _animator = manualAnimator ?? GetComponent<Animator>() ?? GetComponentInChildren<Animator>() ?? GetComponentInParent<Animator>();
            _combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
            FindAttachmentPoint();
        }

        private void Start()
        {
            if (_attachmentPoint == null && _animator != null)
                FindAttachmentPoint();

            var slotCount = useUnifiedWeapons && unifiedSlots != null ? unifiedSlots.Length : (slots?.Length ?? 0);
            if (_currentWeaponIndex < 0 && slotCount > 0)
                EquipWeapon(0);
        }

        private void Update()
        {
            int slotCount = useUnifiedWeapons && unifiedSlots != null
                ? Mathf.Min(4, unifiedSlots.Length)
                : (slots != null ? Mathf.Min(4, slots.Length) : 0);
            if (slotCount == 0) return;

            for (int i = 0; i < slotCount; i++)
            {
                if (GetKeyDownForSlot(i))
                {
                    EquipWeapon(i);
                    break;
                }
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

        private void FindAttachmentPoint()
        {
            if (manualAttachmentPoint != null)
            {
                _attachmentPoint = manualAttachmentPoint;
                return;
            }

            if (_animator == null)
            {
                Debug.LogWarning("[GeisWeaponSwitcher] No Animator found.");
                return;
            }

            foreach (var name in attachmentBoneNames)
            {
                var t = FindTransformRecursive(_animator.transform, name);
                if (t != null)
                {
                    _attachmentPoint = t;
                    return;
                }
            }

            if (useAnimatorRightHandFallback && _animator.avatar != null)
            {
                var rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHand != null)
                {
                    _attachmentPoint = rightHand;
                    return;
                }
            }

            _attachmentPoint = _animator.transform;
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
            Transform parent = _attachmentPoint != null ? _attachmentPoint : transform;
            GameObject prefab = null;

            if (useUnifiedWeapons && unifiedSlots != null && slotIndex >= 0 && slotIndex < unifiedSlots.Length)
            {
                var def = unifiedSlots[slotIndex];
                if (def != null)
                    prefab = def.weaponPrefab;

                if (_combatEntity != null)
                {
                    var data = _combatEntity.GetEntityData();
                    if (data != null && def != null)
                        data.equippedWeapon = def.GetWeaponForDamage();
                }
            }
            else if (slots != null && slotIndex >= 0 && slotIndex < slots.Length)
            {
                var slot = slots[slotIndex];
                if (slot != null)
                    prefab = slot.weaponPrefab;
            }
            else
            {
                return;
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
