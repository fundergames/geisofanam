// Geis of Anam - Weapon equipping via keys 1-4 (Unarmed, Knife, Sword, Bow).
// No CombatEntity dependency; works with GeisPlayerAnimationController.

using UnityEngine;

namespace Geis.Combat
{
    /// <summary>
    /// Defines a weapon slot: prefab to instantiate (null for unarmed) and display name.
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
    /// Sets EquippedWeaponIndex on Animator for combo data lookup.
    /// </summary>
    public class GeisWeaponSwitcher : MonoBehaviour
    {
        [Header("Weapon Slots")]
        [Tooltip("Slots: [0]=Unarmed, [1]=Knife, [2]=Sword, [3]=Bow")]
        [SerializeField]
        private GeisWeaponSlot[] slots = new GeisWeaponSlot[4];

        [Header("Attachment")]
        [Tooltip("Optional: assign manually if auto-detect fails")]
        [SerializeField]
        private Transform manualAttachmentPoint;

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

        private void Awake()
        {
            _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            FindAttachmentPoint();
        }

        private void Start()
        {
            if (_attachmentPoint == null && _animator != null)
                FindAttachmentPoint();

            if (_currentWeaponIndex < 0 && slots != null && slots.Length > 0)
                EquipWeapon(0);
        }

        private void Update()
        {
            if (slots == null) return;

            for (int i = 0; i < Mathf.Min(4, slots.Length); i++)
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
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return;

            Transform parent = _attachmentPoint != null ? _attachmentPoint : transform;

            if (_currentWeaponInstance != null)
            {
                Destroy(_currentWeaponInstance);
                _currentWeaponInstance = null;
            }

            var slot = slots[slotIndex];
            if (slot != null && slot.weaponPrefab != null)
            {
                _currentWeaponInstance = Instantiate(slot.weaponPrefab, parent);
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
