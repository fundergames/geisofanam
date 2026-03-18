using System.Collections.Generic;
using UnityEngine;
using RogueDeal.Items;
using RogueDeal.Player;

namespace RogueDeal.Combat.Visual
{
    /// <summary>
    /// Manages the visual appearance of a character, including body parts and equipment.
    /// This component handles dynamic attachment of body parts and equipment at runtime.
    /// </summary>
    public class CharacterVisualManager : MonoBehaviour
    {
        [Header("Character Configuration")]
        [Tooltip("Visual data asset that defines this character's appearance")]
        [SerializeField] private CharacterVisualData visualData;
        
        [Header("References")]
        [Tooltip("Root transform where body parts will be attached (usually the character root)")]
        [SerializeField] private Transform bodyPartsRoot;
        
        [Tooltip("Animator component (used to find bones for attachment)")]
        [SerializeField] private Animator animator;
        
        [Header("Body Parts")]
        [Tooltip("Currently attached body parts")]
        [SerializeField] private Dictionary<string, GameObject> attachedBodyParts = new Dictionary<string, GameObject>();
        
        [Header("Equipment")]
        [Tooltip("Currently attached equipment")]
        [SerializeField] private Dictionary<EquipmentSlot, GameObject> attachedEquipment = new Dictionary<EquipmentSlot, GameObject>();
        
        [Tooltip("Attachment points found on this character")]
        [SerializeField] private Dictionary<string, EquipmentAttachmentPoint> attachmentPoints = new Dictionary<string, EquipmentAttachmentPoint>();
        
        private PlayerCharacter playerCharacter;
        
        private void Awake()
        {
            InitializeReferences();
            FindAttachmentPoints();
        }
        
        private void InitializeReferences()
        {
            if (bodyPartsRoot == null)
                bodyPartsRoot = transform;
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            // Try to get PlayerCharacter from parent or sibling components
            if (playerCharacter == null)
            {
                var playerVisual = GetComponent<PlayerVisual>();
                if (playerVisual != null)
                    playerCharacter = playerVisual.PlayerCharacter;
            }
        }
        
        private void FindAttachmentPoints()
        {
            attachmentPoints.Clear();
            EquipmentAttachmentPoint[] points = GetComponentsInChildren<EquipmentAttachmentPoint>();
            foreach (var point in points)
            {
                if (!string.IsNullOrEmpty(point.attachmentPointName))
                {
                    attachmentPoints[point.attachmentPointName] = point;
                }
            }
        }
        
        /// <summary>
        /// Initialize the character visual from a CharacterVisualData asset
        /// </summary>
        public void Initialize(CharacterVisualData data, PlayerCharacter character = null)
        {
            visualData = data;
            playerCharacter = character;
            
            if (visualData == null)
            {
                Debug.LogWarning($"[CharacterVisualManager] No visual data provided for {gameObject.name}");
                return;
            }
            
            // Attach body parts
            AttachBodyParts();
            
            // Attach equipment if player character is provided
            if (playerCharacter != null)
            {
                AttachEquipmentFromPlayerCharacter();
            }
        }
        
        /// <summary>
        /// Attach all body parts defined in the visual data
        /// </summary>
        private void AttachBodyParts()
        {
            if (visualData == null || visualData.bodyParts == null)
                return;
            
            foreach (var bodyPartData in visualData.bodyParts)
            {
                if (bodyPartData == null || bodyPartData.bodyPartPrefab == null)
                    continue;
                
                AttachBodyPart(bodyPartData);
            }
        }
        
        /// <summary>
        /// Attach a single body part
        /// </summary>
        public GameObject AttachBodyPart(CharacterBodyPartData bodyPartData)
        {
            if (bodyPartData == null || bodyPartData.bodyPartPrefab == null)
            {
                Debug.LogWarning("[CharacterVisualManager] Cannot attach null body part");
                return null;
            }
            
            // Find attachment bone if specified
            Transform attachParent = bodyPartsRoot;
            if (!string.IsNullOrEmpty(bodyPartData.attachBoneName) && animator != null)
            {
                Transform bone = animator.GetBoneTransform(HumanBodyBones.Hips); // Default
                
                // Try to find bone by name
                bone = FindBoneByName(bodyPartData.attachBoneName);
                if (bone != null)
                {
                    attachParent = bone;
                }
            }
            
            // Remove existing body part of same name if present
            if (attachedBodyParts.ContainsKey(bodyPartData.bodyPartName))
            {
                RemoveBodyPart(bodyPartData.bodyPartName);
            }
            
            // Instantiate body part
            GameObject bodyPart = Instantiate(bodyPartData.bodyPartPrefab, attachParent);
            bodyPart.transform.localPosition = bodyPartData.positionOffset;
            bodyPart.transform.localRotation = Quaternion.Euler(bodyPartData.rotationOffset);
            
            // Apply material override if specified
            if (bodyPartData.materialOverride != null)
            {
                SkinnedMeshRenderer renderer = bodyPart.GetComponent<SkinnedMeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = bodyPartData.materialOverride;
                }
            }
            
            // Set visibility
            bodyPart.SetActive(bodyPartData.visibleByDefault);
            
            attachedBodyParts[bodyPartData.bodyPartName] = bodyPart;
            
            return bodyPart;
        }
        
        /// <summary>
        /// Remove a body part by name
        /// </summary>
        public void RemoveBodyPart(string bodyPartName)
        {
            if (attachedBodyParts.TryGetValue(bodyPartName, out GameObject bodyPart))
            {
                Destroy(bodyPart);
                attachedBodyParts.Remove(bodyPartName);
            }
        }
        
        /// <summary>
        /// Attach equipment from PlayerCharacter's equipment dictionary
        /// </summary>
        private void AttachEquipmentFromPlayerCharacter()
        {
            if (playerCharacter == null || playerCharacter.equipment == null)
                return;
            
            foreach (var kvp in playerCharacter.equipment)
            {
                if (kvp.Value != null)
                {
                    EquipItem(kvp.Value);
                }
            }
        }
        
        /// <summary>
        /// Equip an equipment item
        /// </summary>
        public void EquipItem(EquipmentItem item)
        {
            if (item == null || item.equipmentModel == null)
            {
                Debug.LogWarning("[CharacterVisualManager] Cannot equip null item or item with no model");
                return;
            }
            
            // Find attachment point for this equipment slot
            EquipmentAttachmentPoint attachmentPoint = FindAttachmentPointForSlot(item.slot);
            if (attachmentPoint == null)
            {
                Debug.LogWarning($"[CharacterVisualManager] No attachment point found for slot: {item.slot}");
                return;
            }
            
            // Remove existing equipment in this slot
            UnequipSlot(item.slot);
            
            // Attach equipment
            GameObject equipment = attachmentPoint.AttachEquipment(item.equipmentModel);
            if (equipment != null)
            {
                attachedEquipment[item.slot] = equipment;
                
                // Hide body parts if needed
                if (attachmentPoint.hideBodyPartsWhenEquipped)
                {
                    HideBodyPartsForSlot(item.slot, attachmentPoint.bodyPartsToHide);
                }
            }
        }
        
        /// <summary>
        /// Unequip an equipment slot
        /// </summary>
        public void UnequipSlot(EquipmentSlot slot)
        {
            if (attachedEquipment.TryGetValue(slot, out GameObject equipment))
            {
                Destroy(equipment);
                attachedEquipment.Remove(slot);
            }
            
            // Find attachment point and detach
            EquipmentAttachmentPoint attachmentPoint = FindAttachmentPointForSlot(slot);
            if (attachmentPoint != null)
            {
                attachmentPoint.DetachEquipment();
            }
            
            // Show body parts again
            ShowBodyPartsForSlot(slot);
        }
        
        /// <summary>
        /// Find attachment point for a specific equipment slot
        /// </summary>
        private EquipmentAttachmentPoint FindAttachmentPointForSlot(EquipmentSlot slot)
        {
            foreach (var kvp in attachmentPoints)
            {
                if (kvp.Value.slot == slot)
                {
                    return kvp.Value;
                }
            }
            
            // Try to find by common naming conventions
            string[] commonNames = GetCommonNamesForSlot(slot);
            foreach (var name in commonNames)
            {
                if (attachmentPoints.TryGetValue(name, out EquipmentAttachmentPoint point))
                {
                    return point;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get common attachment point names for an equipment slot
        /// </summary>
        private string[] GetCommonNamesForSlot(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return new[] { "weapon_r", "weapon_l", "weapon", "hand_r", "hand_l" };
                case EquipmentSlot.Helmet:
                    return new[] { "helmet", "head", "hat" };
                case EquipmentSlot.Armor:
                    return new[] { "armor", "chest", "torso" };
                case EquipmentSlot.Arms:
                    return new[] { "arms", "shoulders" };
                case EquipmentSlot.Legs:
                    return new[] { "legs", "pants" };
                case EquipmentSlot.Accessory:
                    return new[] { "accessory", "back", "cloak" };
                default:
                    return new string[0];
            }
        }
        
        /// <summary>
        /// Hide body parts for a specific slot
        /// </summary>
        private void HideBodyPartsForSlot(EquipmentSlot slot, BodyPartCategory[] categoriesToHide)
        {
            if (categoriesToHide == null || categoriesToHide.Length == 0)
                return;
            
            foreach (var kvp in attachedBodyParts)
            {
                // Try to get body part data to check category
                // For now, we'll hide based on name matching
                string bodyPartName = kvp.Key.ToLower();
                foreach (var category in categoriesToHide)
                {
                    if (bodyPartName.Contains(category.ToString().ToLower()))
                    {
                        kvp.Value.SetActive(false);
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Show body parts for a specific slot
        /// </summary>
        private void ShowBodyPartsForSlot(EquipmentSlot slot)
        {
            // Re-enable body parts that might have been hidden
            // This is a simplified version - you may want to track which parts were hidden
            foreach (var kvp in attachedBodyParts)
            {
                kvp.Value.SetActive(true);
            }
        }
        
        /// <summary>
        /// Find a bone by name in the animator's hierarchy
        /// </summary>
        private Transform FindBoneByName(string boneName)
        {
            if (animator == null)
                return null;
            
            // Search in all children
            return FindBoneRecursive(animator.transform, boneName);
        }
        
        private Transform FindBoneRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;
            
            foreach (Transform child in parent)
            {
                Transform found = FindBoneRecursive(child, name);
                if (found != null)
                    return found;
            }
            
            return null;
        }
        
        /// <summary>
        /// Get all currently attached body parts
        /// </summary>
        public Dictionary<string, GameObject> GetAttachedBodyParts()
        {
            return new Dictionary<string, GameObject>(attachedBodyParts);
        }
        
        /// <summary>
        /// Get all currently attached equipment
        /// </summary>
        public Dictionary<EquipmentSlot, GameObject> GetAttachedEquipment()
        {
            return new Dictionary<EquipmentSlot, GameObject>(attachedEquipment);
        }
    }
}

