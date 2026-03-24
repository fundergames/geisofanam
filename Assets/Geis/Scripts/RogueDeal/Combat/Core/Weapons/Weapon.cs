using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Combat.Core.Data
{
    /// <summary>
    /// Weapon configuration. Defines base damage and damage type multipliers.
    /// Example: Fire Sword = Physical 1.0x, Fire 1.2x
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "RogueDeal/Combat/Weapons/Weapon")]
    public class Weapon : ScriptableObject
    {
        [Header("Basic Info")]
        public string weaponName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        
        [Header("Visual")]
        [Tooltip("Prefab to instantiate when this weapon is equipped (3D model)")]
        public GameObject weaponPrefab;
        
        [Header("Weapon Configuration")]
        [Tooltip("Type of weapon slot (TwoHanded, DualWield, SingleHand, Ranged)")]
        public WeaponSlotType slotType = WeaponSlotType.SingleHand;
        
        [Header("Damage")]
        [Tooltip("Base damage of the weapon")]
        public float baseDamage = 50f;
        
        [Header("Range")]
        [Tooltip("Maximum attack range for this weapon (in Unity units)")]
        public float maxRange = 2f;
        
        [Header("Damage Type Multipliers")]
        [Tooltip("Multipliers for different damage types. Key = DamageType, Value = Multiplier")]
        public DamageTypeMultiplier[] damageTypeMultiplierArray = new DamageTypeMultiplier[0];
        
        // Dictionary for fast lookup (built from array)
        private Dictionary<DamageType, float> _multiplierDict;
        
        /// <summary>
        /// Gets the damage multiplier for a specific damage type
        /// </summary>
        public float GetDamageTypeMultiplier(DamageType damageType)
        {
            if (_multiplierDict == null)
            {
                BuildMultiplierDictionary();
            }
            
            return _multiplierDict.TryGetValue(damageType, out float multiplier) ? multiplier : 1f;
        }
        
        /// <summary>
        /// Gets all damage type multipliers as a dictionary
        /// </summary>
        public Dictionary<DamageType, float> damageTypeMultipliers
        {
            get
            {
                if (_multiplierDict == null)
                {
                    BuildMultiplierDictionary();
                }
                return new Dictionary<DamageType, float>(_multiplierDict);
            }
        }
        
        private void BuildMultiplierDictionary()
        {
            if (_multiplierDict == null)
            {
                _multiplierDict = new Dictionary<DamageType, float>();
            }
            else
            {
                _multiplierDict.Clear();
            }
            
            if (damageTypeMultiplierArray != null)
            {
                foreach (var multiplier in damageTypeMultiplierArray)
                {
                    _multiplierDict[multiplier.damageType] = multiplier.multiplier;
                }
            }
        }
        
        private void OnEnable()
        {
            BuildMultiplierDictionary();
        }
        
        /// <summary>
        /// Forces rebuild of the multiplier dictionary (useful for testing or when array is modified at runtime)
        /// </summary>
        public void RebuildMultiplierDictionary()
        {
            _multiplierDict = null;
            BuildMultiplierDictionary();
        }
    }
    
    /// <summary>
    /// Type of weapon slot
    /// </summary>
    public enum WeaponSlotType
    {
        TwoHanded,
        DualWield,
        SingleHand,
        Ranged
    }
    
    /// <summary>
    /// Damage type multiplier entry
    /// </summary>
    [System.Serializable]
    public class DamageTypeMultiplier
    {
        [Tooltip("Type of damage")]
        public DamageType damageType = DamageType.Physical;
        [Tooltip("Multiplier for this damage type (e.g., 1.2 = 120% damage)")]
        public float multiplier = 1f;
    }
}

