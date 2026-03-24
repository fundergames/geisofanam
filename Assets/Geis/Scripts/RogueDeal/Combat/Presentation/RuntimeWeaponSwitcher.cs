using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat.Core.Data;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Allows switching between 1-handed weapons at runtime using keys 1-9.
    /// Attaches weapon prefabs to the character's weapon bone (weapon_r or hand_r).
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    public class RuntimeWeaponSwitcher : MonoBehaviour
    {
        [Header("Weapon Slots")]
        [Tooltip("Weapons available for switching. Index 0 = key 1, index 1 = key 2, etc. Weapon data contains prefab + combat stats.")]
        [SerializeField] private Weapon[] weaponSlots = new Weapon[0];
        
        [Header("Attachment")]
        [Tooltip("Optional: Assign manually if auto-detect fails (e.g. when Avatar loads late)")]
        [SerializeField] private Transform manualWeaponAttachmentPoint;
        
        [Tooltip("Bone names to search for weapon attachment (first match wins)")]
        [SerializeField] private string[] attachmentBoneNames = { "weapon_r", "hand_r", "Weapon" };
        
        [Tooltip("Fallback: use Animator's RightHand bone if no named bone found (requires Avatar)")]
        [SerializeField] private bool useAnimatorRightHandAsFallback = true;
        
        [Header("Hitbox")]
        [Tooltip("Add WeaponHitbox + Collider if prefab doesn't have one")]
        [SerializeField] private bool addHitboxIfMissing = true;
        
        [Tooltip("Hitbox collider size (for auto-added hitbox)")]
        [SerializeField] private Vector3 hitboxSize = new Vector3(0.3f, 0.3f, 1f);
        
        [Tooltip("Hitbox local position offset from weapon root")]
        [SerializeField] private Vector3 hitboxOffset = new Vector3(0, 0, 0.5f);
        
        [Header("Input")]
        [Tooltip("Use keys 1-9 for weapon switching")]
        [SerializeField] private bool enableKeySwitching = true;
        
        private Transform weaponAttachmentPoint;
        private GameObject currentWeaponInstance;
        private int currentWeaponIndex = -1;
        private CombatEntity combatEntity;
        private Animator animator;
        
        private void Awake()
        {
            combatEntity = GetComponent<CombatEntity>();
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            FindWeaponAttachmentPoint();
        }
        
        private void Start()
        {
            // Retry finding attachment point (character/avatar may load after Awake)
            if (weaponAttachmentPoint == null && animator != null)
            {
                FindWeaponAttachmentPoint();
            }
            
            // Equip first weapon by default if we have slots
            if (weaponSlots != null && weaponSlots.Length > 0 && currentWeaponIndex < 0)
            {
                EquipWeapon(0);
            }
        }
        
        private void Update()
        {
            if (!enableKeySwitching || weaponSlots == null)
                return;
            
            // Check keys 1-9
            for (int i = 0; i < Mathf.Min(9, weaponSlots.Length); i++)
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
            if (index < 0 || index > 8) return false;
            var key = (KeyCode)((int)KeyCode.Alpha1 + index);
            try
            {
                return Input.GetKeyDown(key);
            }
            catch (System.InvalidOperationException)
            {
                // New Input System only mode - try Input System
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    var sysKey = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit1 + index);
                    return UnityEngine.InputSystem.Keyboard.current[sysKey].wasPressedThisFrame;
                }
                return false;
            }
        }
        
        /// <summary>
        /// Find the transform where weapons should be attached.
        /// </summary>
        private void FindWeaponAttachmentPoint()
        {
            if (manualWeaponAttachmentPoint != null)
            {
                weaponAttachmentPoint = manualWeaponAttachmentPoint;
                return;
            }
            
            if (animator == null)
            {
                Debug.LogWarning("[RuntimeWeaponSwitcher] No Animator found - cannot find weapon attachment point");
                return;
            }
            
            // Search hierarchy for named bones
            foreach (var boneName in attachmentBoneNames)
            {
                var bone = FindTransformRecursive(animator.transform, boneName);
                if (bone != null)
                {
                    weaponAttachmentPoint = bone;
                    Debug.Log($"[RuntimeWeaponSwitcher] Found attachment point: {boneName}");
                    return;
                }
            }
            
            // Fallback: use Animator's RightHand (requires Avatar to be assigned)
            if (useAnimatorRightHandAsFallback && animator.avatar != null)
            {
                var rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (rightHand != null)
                {
                    weaponAttachmentPoint = rightHand;
                    Debug.Log("[RuntimeWeaponSwitcher] Using RightHand as attachment point");
                    return;
                }
            }
            else if (useAnimatorRightHandAsFallback && animator.avatar == null)
            {
                Debug.LogWarning("[RuntimeWeaponSwitcher] Animator Avatar is null - assign Manual Weapon Attachment Point or ensure character model loads before Awake.");
            }
            
            Debug.LogWarning("[RuntimeWeaponSwitcher] No weapon attachment point found. Weapons will attach to Animator root.");
            weaponAttachmentPoint = animator.transform;
        }
        
        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;
            foreach (Transform child in parent)
            {
                var found = FindTransformRecursive(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }
        
        /// <summary>
        /// Equip weapon at the given slot index (0 = key 1, 1 = key 2, etc.).
        /// </summary>
        public void EquipWeapon(int slotIndex)
        {
            if (weaponSlots == null || slotIndex < 0 || slotIndex >= weaponSlots.Length)
                return;
            
            var weapon = weaponSlots[slotIndex];
            if (weapon == null || weapon.weaponPrefab == null)
            {
                Debug.LogWarning($"[RuntimeWeaponSwitcher] Slot {slotIndex} has no weapon or weapon prefab assigned");
                return;
            }
            
            // Prefab variants can cause InvalidCastException - ensure we have a valid GameObject
            var prefab = weapon.weaponPrefab as GameObject;
            if (prefab == null)
            {
                Debug.LogWarning($"[RuntimeWeaponSwitcher] Slot {slotIndex} weapon prefab is not a GameObject (type: {weapon.weaponPrefab?.GetType().Name}). Use source prefab, not variant.");
                return;
            }
            
            // Use attachment point or fallback to our transform
            Transform parent = weaponAttachmentPoint != null ? weaponAttachmentPoint : transform;
            
            // Remove current weapon
            if (currentWeaponInstance != null)
            {
                Destroy(currentWeaponInstance);
                currentWeaponInstance = null;
            }
            
            // Instantiate new weapon from Weapon data (preserve prefab's local transform for correct orientation)
            currentWeaponInstance = Instantiate(prefab, parent);
            currentWeaponInstance.transform.localPosition = prefab.transform.localPosition;
            currentWeaponInstance.transform.localRotation = prefab.transform.localRotation;
            currentWeaponInstance.transform.localScale = prefab.transform.localScale;
            currentWeaponInstance.name = prefab.name + "_Equipped";
            
            // Ensure hitbox for combat
            if (addHitboxIfMissing)
            {
                EnsureWeaponHitbox(currentWeaponInstance);
            }
            
            currentWeaponIndex = slotIndex;
            
            // Update CombatEntityData with weapon stats
            if (combatEntity != null)
            {
                var entityData = combatEntity.GetEntityData();
                if (entityData != null)
                {
                    entityData.equippedWeapon = weapon;
                }
            }
            
            Debug.Log($"[RuntimeWeaponSwitcher] Equipped: {weapon.weaponName} (slot {slotIndex + 1})");
        }
        
        private void EnsureWeaponHitbox(GameObject weaponRoot)
        {
            var existing = weaponRoot.GetComponentInChildren<WeaponHitbox>();
            if (existing != null)
                return;
            
            // Add hitbox as child
            var hitboxObj = new GameObject("WeaponHitbox");
            hitboxObj.transform.SetParent(weaponRoot.transform, false);
            hitboxObj.transform.localPosition = hitboxOffset;
            hitboxObj.transform.localRotation = Quaternion.identity;
            hitboxObj.transform.localScale = Vector3.one;
            
            var collider = hitboxObj.AddComponent<BoxCollider>();
            collider.size = hitboxSize;
            collider.center = Vector3.zero;
            collider.isTrigger = true;
            collider.enabled = false;
            
            var hitbox = hitboxObj.AddComponent<WeaponHitbox>();
            hitbox.targetLayers = LayerMask.GetMask("Default");
            hitbox.validTargetTags = new[] { "Enemy" };
        }
        
        /// <summary>
        /// Get the currently equipped weapon index (-1 if none).
        /// </summary>
        public int CurrentWeaponIndex => currentWeaponIndex;
        
        /// <summary>
        /// Get the weapon slots (read-only).
        /// </summary>
        public IReadOnlyList<Weapon> WeaponSlots => weaponSlots;
    }
}
