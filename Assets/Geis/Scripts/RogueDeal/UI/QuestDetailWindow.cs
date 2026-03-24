using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RogueDeal.Quests;

namespace RogueDeal.UI
{
    /// <summary>
    /// Small window that displays detailed quest information when a quest icon is clicked.
    /// </summary>
    public class QuestDetailWindow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI objectivesText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject windowPanel;

        [Header("Settings")]
#pragma warning disable CS0414 // Reserved for layout logic
        [SerializeField] private float windowWidth = 300f;
        [SerializeField] private float windowHeight = 400f;
#pragma warning restore CS0414

        private void Awake()
        {
            // Auto-find components
            if (titleText == null)
                titleText = transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();

            if (descriptionText == null)
                descriptionText = transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();

            if (objectivesText == null)
                objectivesText = transform.Find("ObjectivesText")?.GetComponent<TextMeshProUGUI>();

            if (iconImage == null)
                iconImage = transform.Find("IconImage")?.GetComponent<Image>();

            if (closeButton == null)
                closeButton = transform.Find("CloseButton")?.GetComponent<Button>();

            if (windowPanel == null)
                windowPanel = gameObject;

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            // Initially hidden
            if (windowPanel != null)
                windowPanel.SetActive(false);
        }

        public void Show(QuestProgress progress, QuestDefinition definition)
        {
            if (definition == null || progress == null)
                return;

            // Update title
            if (titleText != null)
                titleText.text = definition.displayName ?? "Quest";

            // Update description
            if (descriptionText != null)
                descriptionText.text = definition.description ?? "";

            // Update objectives
            if (objectivesText != null)
                objectivesText.text = FormatObjectives(progress, definition);

            // Update icon
            if (iconImage != null && definition.icon != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.color = Color.white;
            }

            // Show window
            if (windowPanel != null)
                windowPanel.SetActive(true);

            // Position near the clicked icon (will be set by QuestPanel)
        }

        public void Close()
        {
            if (windowPanel != null)
                windowPanel.SetActive(false);
        }

        private string FormatObjectives(QuestProgress progress, QuestDefinition definition)
        {
            if (definition.objectives == null || definition.objectives.Count == 0)
                return "No objectives";

            var lines = new System.Text.StringBuilder();

            foreach (var objective in definition.objectives)
            {
                var objProgress = progress.objectives?.FirstOrDefault(o => o.objectiveId == objective.objectiveId);

                if (objProgress != null)
                {
                    int current = Mathf.Min(objProgress.currentAmount, objective.targetAmount);
                    string status = objProgress.completed ? "✓" : "○";
                    lines.AppendLine($"{status} {objective.description}: {current}/{objective.targetAmount}");
                }
                else
                {
                    lines.AppendLine($"○ {objective.description}: 0/{objective.targetAmount}");
                }
            }

            return lines.ToString().TrimEnd();
        }

        public void SetPosition(Vector2 position)
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = position;
            }
        }
    }
}
