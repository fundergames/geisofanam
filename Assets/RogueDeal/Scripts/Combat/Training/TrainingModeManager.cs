using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using RogueDeal.Events;

namespace RogueDeal.Combat.Training
{
    public class TrainingModeManager : MonoBehaviour
    {
        [Header("Training Settings")]
        [SerializeField] private bool trainingModeActive = false;
        [SerializeField] private float timeScale = 1f;
        [SerializeField] private bool showFrameData = true;
        [SerializeField] private bool showHitboxes = true;
        [SerializeField] private bool infiniteHealth = true;
        
        [Header("Dummy Settings")]
        [SerializeField] private GameObject dummyPrefab;
        [SerializeField] private Transform dummySpawnPoint;
        [SerializeField] private DummyBehavior dummyBehaviorMode = DummyBehavior.Idle;
        
        [Header("Recording")]
        [SerializeField] private bool recordingEnabled = false;
        [SerializeField] private int maxRecordedInputs = 100;
        
        [Header("Visual Feedback")]
#pragma warning disable CS0414
        [SerializeField] private bool showAttackArcs = true;
#pragma warning restore CS0414
        [SerializeField] private Color attackArcColor = new Color(1f, 0f, 0f, 0.3f);
#pragma warning disable CS0414
        [SerializeField] private bool showTimingWindows = true;
#pragma warning restore CS0414

        private GameObject currentDummy;
        private ComboRecorder comboRecorder;
        private TrainingUI trainingUI;
        private TrainingAttackController attackController;
        private List<AttackTimingData> attackTimings = new List<AttackTimingData>();
        private float currentAttackStartTime;
#pragma warning disable CS0414
        private bool isAttacking;
#pragma warning restore CS0414
        
        public bool IsTrainingMode => trainingModeActive;
        public float CurrentTimeScale => timeScale;
        
        private void Awake()
        {
            comboRecorder = gameObject.AddComponent<ComboRecorder>();
            comboRecorder.Initialize(maxRecordedInputs);
            
            attackController = GetComponent<TrainingAttackController>();
            if (attackController == null)
            {
                attackController = gameObject.AddComponent<TrainingAttackController>();
            }
        }
        
        private void OnEnable()
        {
            CombatEvents.OnAttackStarted += OnAttackStarted;
            CombatEvents.OnAttackConnected += OnAttackConnected;
        }
        
        private void OnDisable()
        {
            CombatEvents.OnAttackStarted -= OnAttackStarted;
            CombatEvents.OnAttackConnected -= OnAttackConnected;
        }
        
        private void Update()
        {
            if (trainingModeActive)
            {
                HandleTrainingModeInput();
                Time.timeScale = timeScale;
            }
        }
        
        public void ToggleTrainingMode()
        {
            trainingModeActive = !trainingModeActive;
            
            if (trainingModeActive)
            {
                ActivateTrainingMode();
            }
            else
            {
                DeactivateTrainingMode();
            }
        }
        
        private void ActivateTrainingMode()
        {
            Debug.Log("[TrainingMode] Activated");
            
            if (dummyPrefab != null && dummySpawnPoint != null)
            {
                SpawnDummy();
            }
            
            if (trainingUI != null)
            {
                trainingUI.Show();
            }
        }
        
        private void DeactivateTrainingMode()
        {
            Debug.Log("[TrainingMode] Deactivated");
            Time.timeScale = 1f;
            
            if (currentDummy != null)
            {
                Destroy(currentDummy);
            }
            
            if (trainingUI != null)
            {
                trainingUI.Hide();
            }
        }
        
        private void SpawnDummy()
        {
            if (currentDummy != null)
            {
                Destroy(currentDummy);
            }
            
            currentDummy = Instantiate(dummyPrefab, dummySpawnPoint.position, dummySpawnPoint.rotation);
            
            TrainingDummy dummy = currentDummy.GetComponent<TrainingDummy>();
            if (dummy != null)
            {
                dummy.SetBehavior(dummyBehaviorMode);
                dummy.SetInfiniteHealth(infiniteHealth);
            }
        }
        
        private void HandleTrainingModeInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            
            if (keyboard.f1Key.wasPressedThisFrame)
            {
                AdjustTimeScale(0.25f);
            }
            else if (keyboard.f2Key.wasPressedThisFrame)
            {
                AdjustTimeScale(0.5f);
            }
            else if (keyboard.f3Key.wasPressedThisFrame)
            {
                AdjustTimeScale(0.75f);
            }
            else if (keyboard.f4Key.wasPressedThisFrame)
            {
                AdjustTimeScale(1f);
            }
            
            if (keyboard.f5Key.wasPressedThisFrame)
            {
                ResetDummy();
            }
            
            if (keyboard.f6Key.wasPressedThisFrame)
            {
                CycleDummyBehavior();
            }
            
            if (keyboard.f7Key.wasPressedThisFrame)
            {
                ToggleRecording();
            }
            
            if (keyboard.f8Key.wasPressedThisFrame)
            {
                PlaybackRecording();
            }
            
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                ToggleFrameData();
            }
            
            if (keyboard.f10Key.wasPressedThisFrame)
            {
                ToggleHitboxDisplay();
            }
        }
        
        private void AdjustTimeScale(float scale)
        {
            timeScale = scale;
            Debug.Log($"[TrainingMode] Time scale: {timeScale}x");
        }
        
        private void ResetDummy()
        {
            if (currentDummy != null)
            {
                TrainingDummy dummy = currentDummy.GetComponent<TrainingDummy>();
                dummy?.Reset();
            }
        }
        
        private void CycleDummyBehavior()
        {
            int nextBehavior = ((int)dummyBehaviorMode + 1) % System.Enum.GetValues(typeof(DummyBehavior)).Length;
            dummyBehaviorMode = (DummyBehavior)nextBehavior;
            
            if (currentDummy != null)
            {
                TrainingDummy dummy = currentDummy.GetComponent<TrainingDummy>();
                dummy?.SetBehavior(dummyBehaviorMode);
            }
            
            Debug.Log($"[TrainingMode] Dummy behavior: {dummyBehaviorMode}");
        }
        
        private void ToggleRecording()
        {
            recordingEnabled = !recordingEnabled;
            
            if (recordingEnabled)
            {
                comboRecorder.StartRecording();
                Debug.Log("[TrainingMode] Recording started");
            }
            else
            {
                comboRecorder.StopRecording();
                Debug.Log("[TrainingMode] Recording stopped");
            }
        }
        
        private void PlaybackRecording()
        {
            if (comboRecorder.HasRecording)
            {
                comboRecorder.PlaybackRecording();
                Debug.Log("[TrainingMode] Playing back recording");
            }
        }
        
        private void ToggleFrameData()
        {
            showFrameData = !showFrameData;
            Debug.Log($"[TrainingMode] Frame data display: {showFrameData}");
        }
        
        private void ToggleHitboxDisplay()
        {
            showHitboxes = !showHitboxes;
            Debug.Log($"[TrainingMode] Hitbox display: {showHitboxes}");
        }
        
        private void OnAttackStarted(CombatEventData data)
        {
            if (!trainingModeActive) return;
            
            currentAttackStartTime = Time.time;
            isAttacking = true;
            
            AttackTimingData timingData = new AttackTimingData
            {
                abilityName = data.ability != null ? data.ability.abilityName : "Unknown",
                startTime = currentAttackStartTime,
                startFrame = Time.frameCount
            };
            
            attackTimings.Add(timingData);
        }
        
        private void OnAttackConnected(CombatEventData data)
        {
            if (!trainingModeActive || attackTimings.Count == 0) return;
            
            AttackTimingData currentTiming = attackTimings[attackTimings.Count - 1];
            currentTiming.hitTime = Time.time;
            currentTiming.hitFrame = Time.frameCount;
            currentTiming.totalFrames = currentTiming.hitFrame - currentTiming.startFrame;
            currentTiming.totalTime = currentTiming.hitTime - currentTiming.startTime;
            
            attackTimings[attackTimings.Count - 1] = currentTiming;
            
            if (showFrameData)
            {
                Debug.Log($"[TrainingMode] Attack: {currentTiming.abilityName} | Startup: {currentTiming.totalFrames} frames ({currentTiming.totalTime:F3}s)");
            }
        }
        
        public List<AttackTimingData> GetAttackTimings()
        {
            return new List<AttackTimingData>(attackTimings);
        }
        
        public void ClearTimingData()
        {
            attackTimings.Clear();
        }
        
        public void SetTrainingUI(TrainingUI ui)
        {
            trainingUI = ui;
        }
    }
    
    public enum DummyBehavior
    {
        Idle,
        Block,
        Dodge,
        Counter,
        Random
    }
    
    [System.Serializable]
    public struct AttackTimingData
    {
        public string abilityName;
        public float startTime;
        public float hitTime;
        public float totalTime;
        public int startFrame;
        public int hitFrame;
        public int totalFrames;
    }
}
