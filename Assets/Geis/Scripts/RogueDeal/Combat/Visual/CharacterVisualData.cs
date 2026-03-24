using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Combat;
using RogueDeal.Items;

namespace RogueDeal.Combat.Visual
{
    /// <summary>
    /// Defines the visual appearance of a character, including body parts and default equipment.
    /// This ScriptableObject allows you to create different character configurations.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterVisual_", menuName = "Funder Games/Rogue Deal/Character/Visual Data")]
    public class CharacterVisualData : ScriptableObject
    {
        [Header("Character Info")]
        [Tooltip("Name/ID of this character visual configuration")]
        public string characterName;
        
        [Tooltip("Description of this character")]
        [TextArea(3, 5)]
        public string description;
        
        [Header("Body Parts")]
        [Tooltip("List of body parts that make up this character")]
        public List<CharacterBodyPartData> bodyParts = new List<CharacterBodyPartData>();
        
        [Header("Default Equipment")]
        [Tooltip("Default equipment to equip when character is created (optional)")]
        public List<DefaultEquipmentEntry> defaultEquipment = new List<DefaultEquipmentEntry>();
        
        [Header("Attachment Points")]
        [Tooltip("Prefab containing EquipmentAttachmentPoint components (optional, can be set up in prefab)")]
        public GameObject attachmentPointsPrefab;
        
        [Header("Settings")]
        [Tooltip("Base prefab to use (should have skeleton, Animator, and core components)")]
        public GameObject baseCharacterPrefab;
        
        [Tooltip("Scale multiplier for this character")]
        [Range(0.5f, 2f)]
        public float scaleMultiplier = 1f;
    }
    
    [System.Serializable]
    public class DefaultEquipmentEntry
    {
        public EquipmentSlot slot;
        public EquipmentItem equipmentItem;
    }
}

