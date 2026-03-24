using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RogueDeal.Quests;

namespace RogueDeal.UI
{
    /// <summary>
    /// Component that displays a single quest using the QuestInfo prefab structure.
    /// Attach this to a QuestInfo prefab instance.
    /// </summary>
    public class QuestInfoDisplay : MonoBehaviour
    {
        [Header("Quest Info Prefab References")]
        [SerializeField] private TextMeshProUGUI titleText;  // TextMission
        [SerializeField] private TextMeshProUGUI infoText;   // TextMissionInfo
        [SerializeField] private Image iconImage;            // HomeMisstionIcon

        private QuestProgress _questProgress;
        private QuestDefinition _questDefinition;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (titleText == null)
            {
                Transform titleObj = transform.Find("TextMission");
                if (titleObj != null)
                    titleText = titleObj.GetComponent<TextMeshProUGUI>();
            }

            if (infoText == null)
            {
                Transform infoObj = transform.Find("TextMissionInfo");
                if (infoObj != null)
                    infoText = infoObj.GetComponent<TextMeshProUGUI>();
            }

            if (iconImage == null)
            {
                Transform iconObj = transform.Find("HomeMisstionIcon");
                if (iconObj != null)
                    iconImage = iconObj.GetComponent<Image>();
            }
        }

        public void SetQuest(QuestProgress progress, QuestDefinition definition)
        {
            _questProgress = progress;
            _questDefinition = definition;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (_questDefinition == null || _questProgress == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // Ensure we have references (in case Awake didn't run yet)
            EnsureReferences();

            // Update title
            if (titleText != null)
            {
                titleText.text = _questDefinition.displayName;
            }
            else
            {
                Debug.LogWarning("[QuestInfoDisplay] Title text not found! Make sure 'TextMission' exists in prefab.");
            }

            // Update info text with objective progress
            if (infoText != null)
            {
                infoText.text = FormatObjectiveText();
            }
            else
            {
                Debug.LogWarning("[QuestInfoDisplay] Info text not found! Make sure 'TextMissionInfo' exists in prefab.");
            }

            // Update icon
            if (iconImage != null && _questDefinition.icon != null)
            {
                iconImage.sprite = _questDefinition.icon;
            }

            gameObject.SetActive(true);
        }

        private void EnsureReferences()
        {
            // Re-find references if they're missing
            if (titleText == null)
            {
                Transform titleObj = transform.Find("TextMission");
                if (titleObj != null)
                    titleText = titleObj.GetComponent<TextMeshProUGUI>();
            }

            if (infoText == null)
            {
                Transform infoObj = transform.Find("TextMissionInfo");
                if (infoObj != null)
                    infoText = infoObj.GetComponent<TextMeshProUGUI>();
            }

            if (iconImage == null)
            {
                Transform iconObj = transform.Find("HomeMisstionIcon");
                if (iconObj != null)
                    iconImage = iconObj.GetComponent<Image>();
            }
        }

        private string FormatObjectiveText()
        {
            if (_questDefinition.objectives == null || _questDefinition.objectives.Count == 0)
            {
                return _questDefinition.description;
            }

            var lines = new System.Text.StringBuilder();
            
            foreach (var objective in _questDefinition.objectives)
            {
                var objProgress = _questProgress.objectives?.FirstOrDefault(o => o.objectiveId == objective.objectiveId);
                
                if (objProgress != null)
                {
                    int current = Mathf.Min(objProgress.currentAmount, objective.targetAmount);
                    string status = objProgress.completed ? "✓" : "";
                    lines.AppendLine($"{status} {objective.description}: {current}/{objective.targetAmount}");
                }
                else
                {
                    lines.AppendLine($"{objective.description}: 0/{objective.targetAmount}");
                }
            }

            return lines.ToString().TrimEnd();
        }
    }
}