using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FunderGames.RPG
{
    public class TargetSelectionMenu : MonoBehaviour
    {
        public static event Action<Combatant> OnTargetSelected;

        public GameObject targetButtonPrefab; // Prefab for target buttons
        public Transform targetButtonContainer; // Container to hold target buttons

        // Display targets based on the list provided
        public void DisplayTargets(List<Combatant> validTargets)
        {
            // Clear existing buttons
            ClearTargetMenu();

            // Create a button for each valid target
            foreach (Combatant target in validTargets)
            {
                CreateTargetButton(target);
            }

            // Show the target selection UI
            gameObject.SetActive(true);
        }

        // Hide the target selection UI
        public void HideMenu()
        {
            gameObject.SetActive(false);
            ClearTargetMenu();
        }

        // Create a button for a specific target
        private void CreateTargetButton(Combatant target)
        {
            var buttonObj = Instantiate(targetButtonPrefab, targetButtonContainer);
            var button = buttonObj.GetComponent<Button>();
            var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = target.Name;

            // Add a listener to handle target selection
            button.onClick.AddListener(() => OnTargetSelected?.Invoke(target));
        }

        // Clear all target buttons from the UI
        private void ClearTargetMenu()
        {
            foreach (Transform child in targetButtonContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }
}