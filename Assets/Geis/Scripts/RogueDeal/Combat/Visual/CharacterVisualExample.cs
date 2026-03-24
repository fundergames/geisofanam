using UnityEngine;
using RogueDeal.Combat.Visual;
using RogueDeal.Player;

namespace RogueDeal.Combat.Visual
{
    /// <summary>
    /// Example script showing how to use the CharacterVisualManager system.
    /// Attach this to a GameObject or use as reference for your own code.
    /// </summary>
    public class CharacterVisualExample : MonoBehaviour
    {
        [Header("Character Setup")]
        [Tooltip("Base character prefab (skeleton only)")]
        public GameObject baseCharacterPrefab;
        
        [Tooltip("Character visual data asset")]
        public CharacterVisualData characterVisualData;
        
        [Tooltip("Player character data (optional)")]
        public PlayerCharacter playerCharacter;
        
        [Header("Runtime")]
        [Tooltip("Spawned character instance")]
        private GameObject spawnedCharacter;
        
        /// <summary>
        /// Spawn and initialize a character with the modular visual system
        /// </summary>
        [ContextMenu("Spawn Character")]
        public void SpawnCharacter()
        {
            if (baseCharacterPrefab == null)
            {
                Debug.LogError("[CharacterVisualExample] Base character prefab is not assigned!");
                return;
            }
            
            if (characterVisualData == null)
            {
                Debug.LogError("[CharacterVisualExample] Character visual data is not assigned!");
                return;
            }
            
            // Spawn the base character
            spawnedCharacter = Instantiate(baseCharacterPrefab, transform.position, transform.rotation);
            spawnedCharacter.name = $"Character_{characterVisualData.characterName}";
            
            // Get or add CharacterVisualManager
            CharacterVisualManager visualManager = spawnedCharacter.GetComponent<CharacterVisualManager>();
            if (visualManager == null)
            {
                visualManager = spawnedCharacter.AddComponent<CharacterVisualManager>();
                Debug.LogWarning("[CharacterVisualExample] CharacterVisualManager not found, added it automatically.");
            }
            
            // Initialize the visual system
            visualManager.Initialize(characterVisualData, playerCharacter);
            
            Debug.Log($"[CharacterVisualExample] Spawned character: {spawnedCharacter.name}");
        }
        
        /// <summary>
        /// Example: Equip an item at runtime
        /// </summary>
        public void EquipItemExample(RogueDeal.Items.EquipmentItem item)
        {
            if (spawnedCharacter == null)
            {
                Debug.LogWarning("[CharacterVisualExample] No character spawned yet!");
                return;
            }
            
            CharacterVisualManager visualManager = spawnedCharacter.GetComponent<CharacterVisualManager>();
            if (visualManager != null)
            {
                visualManager.EquipItem(item);
                Debug.Log($"[CharacterVisualExample] Equipped: {item.displayName}");
            }
        }
        
        /// <summary>
        /// Example: Unequip an item at runtime
        /// </summary>
        public void UnequipItemExample(RogueDeal.Combat.EquipmentSlot slot)
        {
            if (spawnedCharacter == null)
            {
                Debug.LogWarning("[CharacterVisualExample] No character spawned yet!");
                return;
            }
            
            CharacterVisualManager visualManager = spawnedCharacter.GetComponent<CharacterVisualManager>();
            if (visualManager != null)
            {
                visualManager.UnequipSlot(slot);
                Debug.Log($"[CharacterVisualExample] Unequipped: {slot}");
            }
        }
        
        private void OnDestroy()
        {
            if (spawnedCharacter != null)
            {
                Destroy(spawnedCharacter);
            }
        }
    }
}

