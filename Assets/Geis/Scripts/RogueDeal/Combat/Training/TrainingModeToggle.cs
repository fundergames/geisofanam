using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueDeal.Combat.Training
{
    public class TrainingModeToggle : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Key toggleKey = Key.F12;
        [SerializeField] private bool startInTrainingMode = false;
        
        [Header("References")]
        [SerializeField] private TrainingModeManager trainingManager;
        
        private void Start()
        {
            if (trainingManager == null)
            {
                trainingManager = FindFirstObjectByType<TrainingModeManager>();
            }
            
            if (startInTrainingMode && trainingManager != null)
            {
                trainingManager.ToggleTrainingMode();
            }
        }
        
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ToggleTrainingMode();
            }
        }
        
        private void ToggleTrainingMode()
        {
            if (trainingManager != null)
            {
                trainingManager.ToggleTrainingMode();
                Debug.Log($"[TrainingModeToggle] Training Mode: {(trainingManager.IsTrainingMode ? "ON" : "OFF")}");
            }
            else
            {
                Debug.LogWarning("[TrainingModeToggle] No TrainingModeManager found in scene!");
            }
        }
        
        public void SetTrainingManager(TrainingModeManager manager)
        {
            trainingManager = manager;
        }
    }
}
