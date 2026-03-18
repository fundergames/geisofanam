using UnityEngine;
using System.Collections.Generic;

namespace FunderGames.RPG.OpenWorld
{
    /// <summary>
    /// Utility script to manage weapon types and their properties
    /// </summary>
    public class WeaponAnimatorSetup : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        [SerializeField] private List<WeaponType> weaponTypes = new List<WeaponType>();
        [SerializeField] private bool autoSetupWeapons = true;
        
        [Header("Animation References")]
        [SerializeField] private RuntimeAnimatorController noWeaponController;
        [SerializeField] private RuntimeAnimatorController swordAndShieldController;
        [SerializeField] private RuntimeAnimatorController twoHandSwordController;
        [SerializeField] private RuntimeAnimatorController spearController;
        [SerializeField] private RuntimeAnimatorController singleSwordController;
        [SerializeField] private RuntimeAnimatorController magicWandController;
        [SerializeField] private RuntimeAnimatorController doubleSwordController;
        [SerializeField] private RuntimeAnimatorController bowAndArrowController;
        
        [Header("Generated Controllers")]
        [SerializeField] private List<RuntimeAnimatorController> availableControllers = new List<RuntimeAnimatorController>();
        
        private WeaponSystem weaponSystem;
        
        private void Start()
        {
            weaponSystem = GetComponent<WeaponSystem>();
            
            if (autoSetupWeapons)
            {
                SetupWeaponControllers();
            }
        }
        
        /// <summary>
        /// Setup weapon controllers and assign them to weapon types
        /// </summary>
        [ContextMenu("Setup Weapon Controllers")]
        public void SetupWeaponControllers()
        {
            if (weaponSystem == null)
            {
                Debug.LogError("WeaponSystem not found! Add this script to the same GameObject as WeaponSystem.");
                return;
            }
            
            // Clear existing list
            availableControllers.Clear();
            
            // Setup each weapon type with its controller
            SetupWeaponType(WeaponCategory.NoWeapon, noWeaponController);
            SetupWeaponType(WeaponCategory.SwordAndShield, swordAndShieldController);
            SetupWeaponType(WeaponCategory.TwoHandSword, twoHandSwordController);
            SetupWeaponType(WeaponCategory.Spear, spearController);
            SetupWeaponType(WeaponCategory.SingleSword, singleSwordController);
            SetupWeaponType(WeaponCategory.MagicWand, magicWandController);
            SetupWeaponType(WeaponCategory.DoubleSword, doubleSwordController);
            SetupWeaponType(WeaponCategory.BowAndArrow, bowAndArrowController);
            
            Debug.Log($"Setup complete! {availableControllers.Count} weapon controllers configured.");
        }
        
        /// <summary>
        /// Setup a specific weapon type with its animator controller
        /// </summary>
        private void SetupWeaponType(WeaponCategory category, RuntimeAnimatorController controller)
        {
            if (controller == null)
            {
                Debug.LogWarning($"No animator controller assigned for {category}");
                return;
            }
            
            // Get the weapon from the weapon system
            WeaponType weapon = weaponSystem.GetWeapon(category);
            if (weapon != null)
            {
                // Assign the controller
                weapon.animatorController = controller;
                availableControllers.Add(controller);
                
                Debug.Log($"Assigned {controller.name} to {category}");
            }
        }
        
        /// <summary>
        /// Get all available controllers
        /// </summary>
        public List<RuntimeAnimatorController> GetAvailableControllers()
        {
            return new List<RuntimeAnimatorController>(availableControllers);
        }
        
        /// <summary>
        /// Get controller for a specific weapon category
        /// </summary>
        public RuntimeAnimatorController GetControllerForWeapon(WeaponCategory category)
        {
            switch (category)
            {
                case WeaponCategory.NoWeapon: return noWeaponController;
                case WeaponCategory.SwordAndShield: return swordAndShieldController;
                case WeaponCategory.TwoHandSword: return twoHandSwordController;
                case WeaponCategory.Spear: return spearController;
                case WeaponCategory.SingleSword: return singleSwordController;
                case WeaponCategory.MagicWand: return magicWandController;
                case WeaponCategory.DoubleSword: return doubleSwordController;
                case WeaponCategory.BowAndArrow: return bowAndArrowController;
                default: return null;
            }
        }
        
        /// <summary>
        /// Check if a controller is available for a weapon category
        /// </summary>
        public bool HasControllerForWeapon(WeaponCategory category)
        {
            return GetControllerForWeapon(category) != null;
        }
        
        /// <summary>
        /// Validate all weapon controllers are assigned
        /// </summary>
        [ContextMenu("Validate Weapon Controllers")]
        public void ValidateWeaponControllers()
        {
            Debug.Log("=== Weapon Controller Validation ===");
            
            bool allValid = true;
            
            if (!HasControllerForWeapon(WeaponCategory.NoWeapon))
            {
                Debug.LogWarning("❌ NoWeapon controller missing");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ NoWeapon controller assigned");
            }
            
            if (!HasControllerForWeapon(WeaponCategory.SwordAndShield))
            {
                Debug.LogWarning("❌ SwordAndShield controller missing");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ SwordAndShield controller assigned");
            }
            
            if (!HasControllerForWeapon(WeaponCategory.TwoHandSword))
            {
                Debug.LogWarning("❌ TwoHandSword controller missing");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ TwoHandSword controller assigned");
            }
            
            if (!HasControllerForWeapon(WeaponCategory.Spear))
            {
                Debug.LogWarning("❌ Spear controller missing");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ Spear controller assigned");
            }
            
            if (!HasControllerForWeapon(WeaponCategory.SingleSword))
            {
                Debug.LogWarning("❌ SingleSword controller missing");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ SingleSword controller assigned");
            }
            
            if (!HasControllerForWeapon(WeaponCategory.MagicWand))
            {
                Debug.LogWarning("❌ MagicWand controller missing");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ MagicWand controller assigned");
            }
            
            if (!HasControllerForWeapon(WeaponCategory.DoubleSword))
            {
                Debug.LogWarning("❌ DoubleSword controller missing");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ DoubleSword controller assigned");
            }
            
            if (!HasControllerForWeapon(WeaponCategory.BowAndArrow))
            {
                Debug.LogWarning("❌ BowAndArrow controller missing");
                allValid = false;
            }
            else
            {
                Debug.Log("✅ BowAndArrow controller assigned");
            }
            
            if (allValid)
            {
                Debug.Log("🎉 All weapon controllers are properly assigned!");
            }
            else
            {
                Debug.LogWarning("⚠ Some weapon controllers are missing. Assign them in the inspector.");
            }
        }
        
        /// <summary>
        /// Clear all controller assignments
        /// </summary>
        [ContextMenu("Clear All Controllers")]
        public void ClearAllControllers()
        {
            noWeaponController = null;
            swordAndShieldController = null;
            twoHandSwordController = null;
            spearController = null;
            singleSwordController = null;
            magicWandController = null;
            doubleSwordController = null;
            bowAndArrowController = null;
            
            availableControllers.Clear();
            
            Debug.Log("Cleared all weapon controller assignments");
        }
    }
}
