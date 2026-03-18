using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FunderGames.RPG
{
    public class ActionMenu : MonoBehaviour
    {
        public Button actionButtonPrefab; // Prefab for action buttons (can be a Unity UI Button)
        public Transform actionButtonContainer; // Parent container for dynamically generated buttons

        public static event Action<CombatAction> OnActionSelected;

        // This method displays the actions for the selected character
        public void DisplayActionsForCharacter(Combatant combatant)
        {
            // Clear any existing buttons
            ClearMenu();

            // Create a button for each available action
            foreach (var action in combatant.AvailableActions)
            {
                var button = Instantiate(actionButtonPrefab, actionButtonContainer);
                button.GetComponentInChildren<TextMeshProUGUI>().text = action.ActionName; // Set button text

                // Add a listener to the button to handle selection
                button.onClick.AddListener(() => OnActionSelected?.Invoke(action));
            }

            // Show the action menu (assuming it's hidden initially)
            gameObject.SetActive(true);
        }

        // Hides the action menu when not needed
        public void HideMenu()
        {
            gameObject.SetActive(false);
        }
        
        private void ClearMenu()
        {
            foreach (Transform child in actionButtonContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}