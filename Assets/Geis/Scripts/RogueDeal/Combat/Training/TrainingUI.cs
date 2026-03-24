using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text;

namespace RogueDeal.Combat.Training
{
    public class TrainingUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject trainingPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI frameDataText;
        [SerializeField] private TextMeshProUGUI dummyStatsText;
        [SerializeField] private TextMeshProUGUI controlsText;
        [SerializeField] private Slider timeScaleSlider;
        
        [Header("Frame Data Display")]
        [SerializeField] private GameObject frameDataPanel;
        [SerializeField] private TextMeshProUGUI attackNameText;
        [SerializeField] private TextMeshProUGUI startupFramesText;
        [SerializeField] private TextMeshProUGUI activeFramesText;
        [SerializeField] private TextMeshProUGUI recoveryFramesText;
        
        private TrainingModeManager trainingManager;
        private TrainingDummy currentDummy;
        private StringBuilder stringBuilder = new StringBuilder();
        
        private const string CONTROLS_TEXT = 
            "<b>Training Mode Controls</b>\n" +
            "F1-F4: Time Scale (0.25x, 0.5x, 0.75x, 1x)\n" +
            "F5: Reset Dummy\n" +
            "F6: Cycle Dummy Behavior\n" +
            "F7: Toggle Recording\n" +
            "F8: Playback Recording\n" +
            "F9: Toggle Frame Data\n" +
            "F10: Toggle Hitbox Display";
        
        private void Awake()
        {
            if (controlsText != null)
            {
                controlsText.text = CONTROLS_TEXT;
            }
            
            if (timeScaleSlider != null)
            {
                timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
            }
        }
        
        public void Initialize(TrainingModeManager manager)
        {
            trainingManager = manager;
            trainingManager.SetTrainingUI(this);
        }
        
        public void SetDummy(TrainingDummy dummy)
        {
            currentDummy = dummy;
        }
        
        private void Update()
        {
            if (trainingManager == null || !trainingManager.IsTrainingMode)
                return;
            
            UpdateStatusText();
            UpdateDummyStats();
            UpdateFrameData();
        }
        
        private void UpdateStatusText()
        {
            if (statusText == null) return;
            
            stringBuilder.Clear();
            stringBuilder.AppendLine($"<b>Training Mode Active</b>");
            stringBuilder.AppendLine($"Time Scale: {trainingManager.CurrentTimeScale:F2}x");
            stringBuilder.AppendLine($"Frame: {Time.frameCount}");
            stringBuilder.AppendLine($"Time: {Time.time:F2}s");
            
            statusText.text = stringBuilder.ToString();
        }
        
        private void UpdateDummyStats()
        {
            if (dummyStatsText == null || currentDummy == null) return;
            
            stringBuilder.Clear();
            stringBuilder.AppendLine($"<b>Dummy Stats</b>");
            stringBuilder.AppendLine($"Hits Taken: {currentDummy.HitCount}");
            stringBuilder.AppendLine($"Health: {currentDummy.CurrentHealth:F0}/{currentDummy.MaxHealth:F0}");
            
            dummyStatsText.text = stringBuilder.ToString();
        }
        
        private void UpdateFrameData()
        {
            if (frameDataText == null || trainingManager == null) return;
            
            var timings = trainingManager.GetAttackTimings();
            if (timings.Count == 0) return;
            
            stringBuilder.Clear();
            stringBuilder.AppendLine("<b>Recent Attack Data</b>");
            
            int displayCount = Mathf.Min(5, timings.Count);
            for (int i = timings.Count - displayCount; i < timings.Count; i++)
            {
                var timing = timings[i];
                stringBuilder.AppendLine($"{timing.abilityName}: {timing.totalFrames}f ({timing.totalTime:F3}s)");
            }
            
            frameDataText.text = stringBuilder.ToString();
        }
        
        public void DisplayAttackFrameData(string attackName, int startup, int active, int recovery)
        {
            if (frameDataPanel == null) return;
            
            if (attackNameText != null)
                attackNameText.text = attackName;
            
            if (startupFramesText != null)
                startupFramesText.text = $"Startup: {startup}f";
            
            if (activeFramesText != null)
                activeFramesText.text = $"Active: {active}f";
            
            if (recoveryFramesText != null)
                recoveryFramesText.text = $"Recovery: {recovery}f";
            
            frameDataPanel.SetActive(true);
        }
        
        private void OnTimeScaleChanged(float value)
        {
            if (trainingManager != null)
            {
                Time.timeScale = value;
            }
        }
        
        public void Show()
        {
            if (trainingPanel != null)
            {
                trainingPanel.SetActive(true);
            }
        }
        
        public void Hide()
        {
            if (trainingPanel != null)
            {
                trainingPanel.SetActive(false);
            }
        }
        
        public void OnResetButtonClicked()
        {
            if (currentDummy != null)
            {
                currentDummy.Reset();
            }
            
            if (trainingManager != null)
            {
                trainingManager.ClearTimingData();
            }
        }
        
        public void OnClearRecordingButtonClicked()
        {
            ComboRecorder recorder = trainingManager?.GetComponent<ComboRecorder>();
            recorder?.ClearRecording();
        }
    }
}
