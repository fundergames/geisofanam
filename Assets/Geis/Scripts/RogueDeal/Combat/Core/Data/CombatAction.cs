using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using RogueDeal.Combat.Core.Effects;
using RogueDeal.Combat.Core.Cooldowns;
using RogueDeal.Combat.Core.Targeting;

namespace RogueDeal.Combat.Core.Data
{
    /// <summary>
    /// Weapon/action type for animator routing (sword, bow, etc.).
    /// </summary>
    public enum WeaponType
    {
        Sword = 0,
        Bow = 1,
        Unarmed = 2,
        Other = 3
    }

    /// <summary>
    /// Enhanced combat action. Replaces AbilityData with full support for:
    /// - Animation-driven timing
    /// - Composable effects
    /// - Flexible targeting
    /// - Cooldowns and charges
    /// - Combos, projectiles, persistent AOE
    /// </summary>
    [CreateAssetMenu(fileName = "Action_", menuName = "RogueDeal/Combat/Actions/Combat Action")]
    public class CombatAction : ScriptableObject
    {
        [Header("Basic Info")]
        public string actionName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        
        [Header("Animation")]
        [Tooltip("Weapon type for animator routing (Sword, Bow, Other). Maps to ActionType/ActionIndex.")]
        public WeaponType weaponType = WeaponType.Sword;
        [Tooltip("Animation trigger name to play (for simple animations). Overrides weaponType-based trigger when set.")]
        public string animationTrigger;
        
        [Tooltip("Animation clips for combo attacks (played sequentially) - deprecated, use timeline instead")]
        public AnimationClip[] comboAnimations;
        
        [Tooltip("Timeline asset for complex combo sequences (preferred over comboAnimations)")]
        public TimelineAsset timelineAsset;
        
        [Header("Targeting")]
        [Tooltip("Targeting strategy for this action")]
        public TargetingStrategy targetingStrategy;
        
        [Header("Effects")]
        [Tooltip("Effects to apply when this action hits")]
        public BaseEffect[] effects;
        
        [Header("Combo Data")]
        [Tooltip("Is this a multi-hit combo attack?")]
        public bool isCombo = false;
        
        [Tooltip("Number of hits in the combo")]
        public int comboHitCount = 1;
        
        [Tooltip("Optional per-hit effects (if different from main effects)")]
        public BaseEffect[] perHitEffects;
        
        [Header("Projectile")]
        [Tooltip("Does this action spawn a projectile?")]
        public bool isProjectile = false;
        
        [Tooltip("Projectile prefab to spawn")]
        public GameObject projectilePrefab;
        
        [Tooltip("Speed of the projectile")]
        public float projectileSpeed = 10f;
        
        [Header("Persistent AOE")]
        [Tooltip("Does this action spawn a persistent AOE zone?")]
        public bool spawnsPersistentAOE = false;
        
        [Tooltip("Persistent AOE prefab to spawn")]
        public GameObject persistentAOEPrefab;
        
        [Tooltip("Radius of the AOE")]
        public float aoeRadius = 5f;
        
        [Tooltip("Number of pulses")]
        public int pulseCount = 5;
        
        [Tooltip("Duration between pulses (in seconds)")]
        public float pulseDuration = 1f;
        
        [Header("Cooldown")]
        [Tooltip("Cooldown configuration")]
        public CooldownConfiguration cooldownConfig;
        
        [Header("Visual Effects")]
        [Tooltip("Maps animation event names to VFX/SFX")]
        public EffectBinding[] effectBindings;
    }
    
    /// <summary>
    /// Binds animation events to visual/audio effects
    /// </summary>
    [System.Serializable]
    public class EffectBinding
    {
        [Tooltip("Animation event name (e.g., 'SpawnVFX')")]
        public string eventName;
        
        [Tooltip("VFX prefab to spawn")]
        public GameObject vfxPrefab;
        
        [Tooltip("SFX to play")]
        public AudioClip sfx;
    }
}

