using UnityEngine;
using RogueDeal.Combat;

namespace RogueDeal.Combat.Visual
{
    /// <summary>
    /// Defines an attachment point on a character where equipment can be attached.
    /// This component should be placed on the bone where equipment should attach.
    /// </summary>
    public class EquipmentAttachmentPoint : MonoBehaviour
    {
        [Header("Attachment Info")]
        [Tooltip("Name/ID of this attachment point (e.g., 'weapon_r', 'helmet', 'chest')")]
        public string attachmentPointName;
        
        [Tooltip("Type of equipment that can attach here")]
        public EquipmentSlot slot;
        
        [Header("Transform Settings")]
        [Tooltip("Local position offset for attached equipment")]
        public Vector3 positionOffset = Vector3.zero;
        
        [Tooltip("Local rotation offset for attached equipment")]
        public Vector3 rotationOffset = Vector3.zero;
        
        [Tooltip("Local scale for attached equipment")]
        public Vector3 scale = Vector3.one;
        
        [Header("Options")]
        [Tooltip("Should body parts be hidden when equipment is attached here?")]
        public bool hideBodyPartsWhenEquipped = false;
        
        [Tooltip("Body part categories to hide when equipment is attached")]
        public BodyPartCategory[] bodyPartsToHide;
        
        private GameObject attachedEquipment;
        
        /// <summary>
        /// Attach equipment to this point
        /// </summary>
        public GameObject AttachEquipment(GameObject equipmentPrefab)
        {
            if (equipmentPrefab == null)
            {
                Debug.LogWarning($"[EquipmentAttachmentPoint] Cannot attach null equipment to {attachmentPointName}");
                return null;
            }
            
            // Remove existing equipment
            DetachEquipment();
            
            // Instantiate and attach
            attachedEquipment = Instantiate(equipmentPrefab, transform);
            attachedEquipment.transform.localPosition = positionOffset;
            attachedEquipment.transform.localRotation = Quaternion.Euler(rotationOffset);
            attachedEquipment.transform.localScale = scale;
            attachedEquipment.name = $"{equipmentPrefab.name}_Attached";
            
            return attachedEquipment;
        }
        
        /// <summary>
        /// Detach equipment from this point
        /// </summary>
        public void DetachEquipment()
        {
            if (attachedEquipment != null)
            {
                Destroy(attachedEquipment);
                attachedEquipment = null;
            }
        }
        
        /// <summary>
        /// Check if equipment is currently attached
        /// </summary>
        public bool HasEquipmentAttached()
        {
            return attachedEquipment != null;
        }
        
        /// <summary>
        /// Get the currently attached equipment
        /// </summary>
        public GameObject GetAttachedEquipment()
        {
            return attachedEquipment;
        }
    }
}

