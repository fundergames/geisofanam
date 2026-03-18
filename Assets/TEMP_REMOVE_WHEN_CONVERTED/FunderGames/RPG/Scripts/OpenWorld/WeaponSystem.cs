using UnityEngine;
using System.Collections.Generic;

namespace FunderGames.RPG.OpenWorld
{
    /// <summary>
    /// Generic weapon system that works with all weapon animation sets
    /// </summary>
    [System.Serializable]
    public class WeaponType
    {
        [Header("Weapon Info")]
        public string weaponName;
        public WeaponCategory category;
        public GameObject weaponModel;
        
        [Header("Animation Settings")]
        public RuntimeAnimatorController animatorController;
        public bool useRootMotion = true;
        
        [Header("Combat Stats")]
        public float attackDamage = 25f;
        public float attackRange = 2f;
        public float attackSpeed = 1f;
        public float attackCooldown = 0.5f;
        
        [Header("Movement Multipliers")]
        public float walkSpeedMultiplier = 1f;
        public float runSpeedMultiplier = 1f;
        public float jumpHeightMultiplier = 1f;
    }
    
    public enum WeaponCategory
    {
        NoWeapon,
        SwordAndShield,
        TwoHandSword,
        Spear,
        SingleSword,
        MagicWand,
        DoubleSword,
        BowAndArrow
    }
    
    /// <summary>
    /// Main weapon system component that manages weapon switching and animation
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        [SerializeField] private List<WeaponType> availableWeapons = new List<WeaponType>();
        [SerializeField] private int currentWeaponIndex = 0;
        
        [Header("Animation References")]
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private Transform weaponParent;
        
        [Header("Auto-Setup")]
        [SerializeField] private bool autoSetupWeapons = true;
#pragma warning disable CS0414
        [SerializeField] private string weaponAnimationsPath = "Assets/RPGTinyHeroWavePBR/Animation";
#pragma warning restore CS0414
        
        // Components
        private PlayerController playerController;
        private HealthSystem playerHealth;
        
        // Current weapon
        private WeaponType currentWeapon;
        private GameObject currentWeaponModel;
        
        // Events
        public System.Action<WeaponType> OnWeaponChanged;
        public System.Action<WeaponType> OnWeaponEquipped;
        
        private void Awake()
        {
            // Get components
            playerController = GetComponent<PlayerController>();
            playerHealth = GetComponent<HealthSystem>();
            
            if (characterAnimator == null)
                characterAnimator = GetComponentInChildren<Animator>();
            
            if (weaponParent == null)
            {
                // Create weapon parent if none exists
                GameObject weaponParentObj = new GameObject("WeaponParent");
                weaponParentObj.transform.SetParent(transform);
                weaponParentObj.transform.localPosition = Vector3.zero;
                weaponParent = weaponParentObj.transform;
            }
        }
        
        private void Start()
        {
            if (autoSetupWeapons)
            {
                AutoSetupWeapons();
            }
            
            // Equip default weapon
            if (availableWeapons.Count > 0)
            {
                EquipWeapon(currentWeaponIndex);
            }
        }
        
        /// <summary>
        /// Automatically setup weapons based on the animation folder structure
        /// </summary>
        private void AutoSetupWeapons()
        {
            availableWeapons.Clear();
            
            // Create weapon types for each category
            foreach (WeaponCategory category in System.Enum.GetValues(typeof(WeaponCategory)))
            {
                WeaponType weapon = CreateWeaponType(category);
                availableWeapons.Add(weapon);
            }
            
            Debug.Log($"Auto-setup complete! Found {availableWeapons.Count} weapon types.");
        }
        
        /// <summary>
        /// Create a weapon type with default settings for a category
        /// </summary>
        private WeaponType CreateWeaponType(WeaponCategory category)
        {
            WeaponType weapon = new WeaponType
            {
                weaponName = category.ToString(),
                category = category,
                useRootMotion = true
            };
            
            // Set category-specific defaults
            switch (category)
            {
                case WeaponCategory.NoWeapon:
                    weapon.attackDamage = 20f;
                    weapon.attackRange = 1.5f;
                    weapon.attackSpeed = 1.2f;
                    weapon.walkSpeedMultiplier = 1.1f;
                    weapon.runSpeedMultiplier = 1.1f;
                    break;
                    
                case WeaponCategory.SwordAndShield:
                    weapon.attackDamage = 25f;
                    weapon.attackRange = 2f;
                    weapon.attackSpeed = 1f;
                    weapon.walkSpeedMultiplier = 0.9f;
                    weapon.runSpeedMultiplier = 0.8f;
                    break;
                    
                case WeaponCategory.TwoHandSword:
                    weapon.attackDamage = 35f;
                    weapon.attackRange = 2.5f;
                    weapon.attackSpeed = 0.8f;
                    weapon.walkSpeedMultiplier = 0.8f;
                    weapon.runSpeedMultiplier = 0.7f;
                    break;
                    
                case WeaponCategory.Spear:
                    weapon.attackDamage = 30f;
                    weapon.attackRange = 3f;
                    weapon.attackSpeed = 0.9f;
                    weapon.walkSpeedMultiplier = 0.95f;
                    weapon.runSpeedMultiplier = 0.85f;
                    break;
                    
                case WeaponCategory.SingleSword:
                    weapon.attackDamage = 28f;
                    weapon.attackRange = 2.2f;
                    weapon.attackSpeed = 1.1f;
                    weapon.walkSpeedMultiplier = 1f;
                    weapon.runSpeedMultiplier = 0.9f;
                    break;
                    
                case WeaponCategory.MagicWand:
                    weapon.attackDamage = 40f;
                    weapon.attackRange = 4f;
                    weapon.attackSpeed = 0.7f;
                    weapon.walkSpeedMultiplier = 1f;
                    weapon.runSpeedMultiplier = 0.9f;
                    break;
                    
                case WeaponCategory.DoubleSword:
                    weapon.attackDamage = 32f;
                    weapon.attackRange = 2.3f;
                    weapon.attackSpeed = 1.2f;
                    weapon.walkSpeedMultiplier = 0.9f;
                    weapon.runSpeedMultiplier = 0.8f;
                    break;
                    
                case WeaponCategory.BowAndArrow:
                    weapon.attackDamage = 45f;
                    weapon.attackRange = 8f;
                    weapon.attackSpeed = 0.6f;
                    weapon.walkSpeedMultiplier = 0.85f;
                    weapon.runSpeedMultiplier = 0.75f;
                    break;
            }
            
            return weapon;
        }
        
        /// <summary>
        /// Equip a weapon by index
        /// </summary>
        public void EquipWeapon(int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= availableWeapons.Count)
            {
                Debug.LogWarning($"Invalid weapon index: {weaponIndex}");
                return;
            }
            
            // Unequip current weapon
            UnequipCurrentWeapon();
            
            // Set new weapon
            currentWeaponIndex = weaponIndex;
            currentWeapon = availableWeapons[weaponIndex];
            
            // Update player controller with new weapon stats
            if (playerController != null)
            {
                UpdatePlayerControllerStats();
            }
            
            // Update animator controller
            if (characterAnimator != null && currentWeapon.animatorController != null)
            {
                characterAnimator.runtimeAnimatorController = currentWeapon.animatorController;
            }
            
            // Spawn weapon model
            if (currentWeapon.weaponModel != null)
            {
                currentWeaponModel = Instantiate(currentWeapon.weaponModel, weaponParent);
                currentWeaponModel.transform.localPosition = Vector3.zero;
                currentWeaponModel.transform.localRotation = Quaternion.identity;
            }
            
            // Trigger events
            OnWeaponChanged?.Invoke(currentWeapon);
            OnWeaponEquipped?.Invoke(currentWeapon);
            
            Debug.Log($"Equipped weapon: {currentWeapon.weaponName}");
        }
        
        /// <summary>
        /// Equip a weapon by category
        /// </summary>
        public void EquipWeapon(WeaponCategory category)
        {
            for (int i = 0; i < availableWeapons.Count; i++)
            {
                if (availableWeapons[i].category == category)
                {
                    EquipWeapon(i);
                    return;
                }
            }
            
            Debug.LogWarning($"Weapon category not found: {category}");
        }
        
        /// <summary>
        /// Unequip current weapon
        /// </summary>
        private void UnequipCurrentWeapon()
        {
            if (currentWeaponModel != null)
            {
                DestroyImmediate(currentWeaponModel);
                currentWeaponModel = null;
            }
        }
        
        /// <summary>
        /// Update player controller stats based on current weapon
        /// </summary>
        private void UpdatePlayerControllerStats()
        {
            if (currentWeapon == null) return;
            
            // Update attack stats
            playerController.SetAttackStats(
                currentWeapon.attackDamage,
                currentWeapon.attackRange,
                currentWeapon.attackCooldown
            );
            
            // Update movement multipliers
            playerController.SetMovementMultipliers(
                currentWeapon.walkSpeedMultiplier,
                currentWeapon.runSpeedMultiplier,
                currentWeapon.jumpHeightMultiplier
            );
        }
        
        /// <summary>
        /// Switch to next weapon
        /// </summary>
        public void NextWeapon()
        {
            int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Count;
            EquipWeapon(nextIndex);
        }
        
        /// <summary>
        /// Switch to previous weapon
        /// </summary>
        public void PreviousWeapon()
        {
            int prevIndex = (currentWeaponIndex - 1 + availableWeapons.Count) % availableWeapons.Count;
            EquipWeapon(prevIndex);
        }
        
        /// <summary>
        /// Get current weapon
        /// </summary>
        public WeaponType GetCurrentWeapon()
        {
            return currentWeapon;
        }
        
        /// <summary>
        /// Get all available weapons
        /// </summary>
        public List<WeaponType> GetAvailableWeapons()
        {
            return new List<WeaponType>(availableWeapons);
        }
        
        /// <summary>
        /// Check if a weapon category is available
        /// </summary>
        public bool HasWeapon(WeaponCategory category)
        {
            return availableWeapons.Exists(w => w.category == category);
        }
        
        /// <summary>
        /// Get weapon by category
        /// </summary>
        public WeaponType GetWeapon(WeaponCategory category)
        {
            return availableWeapons.Find(w => w.category == category);
        }
        
        // Input methods for weapon switching
        public void OnWeaponSwitch(UnityEngine.InputSystem.InputValue value)
        {
            float scroll = value.Get<float>();
            if (scroll > 0)
                NextWeapon();
            else if (scroll < 0)
                PreviousWeapon();
        }
        
        public void OnWeapon1() => EquipWeapon(WeaponCategory.NoWeapon);
        public void OnWeapon2() => EquipWeapon(WeaponCategory.SwordAndShield);
        public void OnWeapon3() => EquipWeapon(WeaponCategory.TwoHandSword);
        public void OnWeapon4() => EquipWeapon(WeaponCategory.Spear);
        public void OnWeapon5() => EquipWeapon(WeaponCategory.SingleSword);
        public void OnWeapon6() => EquipWeapon(WeaponCategory.MagicWand);
        public void OnWeapon7() => EquipWeapon(WeaponCategory.DoubleSword);
        public void OnWeapon8() => EquipWeapon(WeaponCategory.BowAndArrow);
        
        // Gizmos for debugging
        private void OnDrawGizmosSelected()
        {
            if (currentWeapon != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, currentWeapon.attackRange);
            }
        }
    }
}
