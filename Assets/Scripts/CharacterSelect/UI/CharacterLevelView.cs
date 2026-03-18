using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RogueDeal.UI
{
    public class CharacterLevelView : MonoBehaviour
    { 
        [SerializeField] private Slider levelSlider;
        [SerializeField] private TextMeshProUGUI levelNumberText;
        [SerializeField] private TextMeshProUGUI levelLabelText;
        [SerializeField] private TextMeshProUGUI experienceText;

        public void UpdateDisplay(int level, string levelLabel, string experienceProgress)
        {
            Debug.Log($"[CharacterLevelView] UpdateDisplay called - Level: {level}, Label: {levelLabel}, XP: {experienceProgress}");
            
            if (levelNumberText != null)
            {
                levelNumberText.text = level.ToString();
            }

            if (levelLabelText != null)
            {
                levelLabelText.text = levelLabel;
            }

            if (experienceText != null)
            {
                experienceText.text = experienceProgress;
            }

            if (levelSlider != null)
            {
                levelSlider.value = 0.5f;
            }
        }
    }
}
