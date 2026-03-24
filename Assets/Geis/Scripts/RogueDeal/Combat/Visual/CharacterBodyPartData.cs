using UnityEngine;

namespace RogueDeal.Combat.Visual
{
    /// <summary>
    /// Categories for body parts to help with organization and swapping
    /// </summary>
    public enum BodyPartCategory
    {
        Head,
        Torso,
        Arms,
        Legs,
        Hands,
        Feet,
        Hair,
        FacialHair,
        Other
    }
    
    /// <summary>
    /// Defines a body part that can be attached to a character.
    /// Body parts are typically SkinnedMeshRenderer components that share the same skeleton.
    /// </summary>
    [CreateAssetMenu(fileName = "BodyPart_", menuName = "Funder Games/Rogue Deal/Character/Body Part")]
    public class CharacterBodyPartData : ScriptableObject
    {
        [Header("Body Part Info")]
        [Tooltip("Name/ID of this body part (e.g., 'Head', 'Torso', 'Legs')")]
        public string bodyPartName;
        
        [Tooltip("Category of body part (for organization and swapping)")]
        public BodyPartCategory category;
        
        [Header("Visual")]
        [Tooltip("Prefab containing the SkinnedMeshRenderer for this body part")]
        public GameObject bodyPartPrefab;
        
        [Tooltip("Optional: Material override for this body part")]
        public Material materialOverride;
        
        [Header("Attachment")]
        [Tooltip("Bone name to attach this body part to (if different from root)")]
        public string attachBoneName;
        
        [Tooltip("Local position offset when attaching")]
        public Vector3 positionOffset = Vector3.zero;
        
        [Tooltip("Local rotation offset when attaching")]
        public Vector3 rotationOffset = Vector3.zero;
        
        [Header("Settings")]
        [Tooltip("Should this body part be visible by default?")]
        public bool visibleByDefault = true;
        
        [Tooltip("Can this body part be hidden when equipment is worn?")]
        public bool canBeHiddenByEquipment = true;
    }
}

