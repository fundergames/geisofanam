using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RogueDeal.Player;

namespace RogueDeal.UI
{
    public class ClassSelectionView : MonoBehaviour
    {
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private Transform content;

        private List<ClassButton> buttons = new();

        public void UpdateDisplay(List<HeroData> heroes, System.Action<HeroData> onHeroSelected)
        {
            Debug.Log($"[ClassSelectionView] UpdateDisplay called with {heroes.Count} heroes");
            buttons.Clear();
            foreach (Transform child in content)
            {
                Destroy(child.gameObject);
            }

            foreach (var hero in heroes)
            {
                var buttonObj = Instantiate(buttonPrefab, content);
                var button = buttonObj.GetComponent<ClassButton>();
                if (button != null)
                {
                    Debug.Log($"[ClassSelectionView] Creating button for {hero.PlayerName}");
                    buttons.Add(button);
                    
                    if (button.button == null)
                    {
                        button.button = buttonObj.GetComponent<Button>();
                        Debug.Log($"[ClassSelectionView] Button component was null, manually assigned: {button.button != null}");
                    }
                    
                    button.UpdateDisplay(hero.CharacterClass.Icon, hero.CharacterClass.ClassDisplayName, hero.CharacterClass.ClassDisplayName);
                    
                    if (button.button != null)
                    {
                        button.button.onClick.AddListener(() => {
                            Debug.Log($"[ClassSelectionView] Button clicked for {hero.PlayerName}");
                            onHeroSelected(hero);
                        });
                        button.button.onClick.AddListener(() => SetSelectedButton(button.button));
                    }
                    else
                    {
                        Debug.LogError($"[ClassSelectionView] Button component is NULL for {hero.PlayerName}!");
                    }
                }
                else
                {
                    Debug.LogError($"[ClassSelectionView] ClassButton component not found on prefab!");
                }
            }
            
            if (buttons.Count > 0 && buttons[0].button != null)
            {
                SetSelectedButton(buttons[0].button);
            }
        }

        private void SetSelectedButton(Button selectedButton)
        {
            foreach (var classButton in buttons)
            {
                var isSelected = classButton.button == selectedButton;
                classButton.SetSelected(isSelected);
            }
        }
    }
}
