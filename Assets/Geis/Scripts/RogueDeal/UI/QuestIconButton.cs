using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using RogueDeal.Quests;

namespace RogueDeal.UI
{
    /// <summary>
    /// Displays a quest as a compact circular icon with progress indicator.
    /// Clicking it shows the quest details window.
    /// </summary>
    public class QuestIconButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("Icon Display")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Button button;

        [Header("Settings")]
        [SerializeField] private float iconSize = 80f;
        [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color completedColor = new Color(0.5f, 1f, 0.5f, 0.8f);

        private QuestProgress _questProgress;
        private QuestDefinition _questDefinition;
        private QuestDetailWindow _detailWindow;
        private QuestPanel _questPanel;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (iconImage == null)
                iconImage = GetComponentInChildren<Image>();

            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(OnButtonClicked);

            // Find or create progress text
            if (progressText == null)
            {
                var textObj = transform.Find("ProgressText");
                if (textObj != null)
                    progressText = textObj.GetComponent<TextMeshProUGUI>();
            }

            // Find QuestPanel parent
            _questPanel = GetComponentInParent<QuestPanel>();
        }

        public void SetQuest(QuestProgress progress, QuestDefinition definition, QuestDetailWindow detailWindowPrefab)
        {
            _questProgress = progress;
            _questDefinition = definition;
            _detailWindow = detailWindowPrefab;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (_questDefinition == null || _questProgress == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // Update icon
            if (iconImage != null)
            {
                if (_questDefinition.icon != null)
                {
                    iconImage.sprite = _questDefinition.icon;
                    iconImage.color = Color.white;
                }
                else
                {
                    // Use a default icon or hide
                    iconImage.color = new Color(1f, 1f, 1f, 0.3f);
                }
            }

            // Update progress text
            if (progressText != null)
            {
                string progressStr = CalculateProgress();
                progressText.text = progressStr;
            }

            // Update background color based on completion
            if (backgroundImage != null)
            {
                bool allCompleted = _questProgress.objectives?.All(o => o.completed) ?? false;
                backgroundImage.color = allCompleted ? completedColor : defaultColor;
            }

            // Set size
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            }

            gameObject.SetActive(true);
        }

        private string CalculateProgress()
        {
            if (_questDefinition.objectives == null || _questDefinition.objectives.Count == 0)
            {
                return _questProgress.status == QuestStatus.Completed ? "✓" : "?";
            }

            // Calculate overall progress
            int totalCompleted = 0;
            int totalObjectives = _questDefinition.objectives.Count;

            foreach (var objective in _questDefinition.objectives)
            {
                var objProgress = _questProgress.objectives?.FirstOrDefault(o => o.objectiveId == objective.objectiveId);
                if (objProgress != null && objProgress.completed)
                {
                    totalCompleted++;
                }
            }

            // Show format like "1/2" or "0/1"
            return $"{totalCompleted}/{totalObjectives}";
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnButtonClicked();
        }

        private void OnButtonClicked()
        {
            if (_questPanel != null && _questDefinition != null && _questProgress != null)
            {
                _questPanel.ShowQuestDetails(_questProgress, _questDefinition);
            }
        }
    }
}
