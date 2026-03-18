using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace RogueDeal.Combat.Training
{
    public class ComboRecorder : MonoBehaviour
    {
        private List<RecordedInput> recordedInputs = new List<RecordedInput>();
        private bool isRecording = false;
        private bool isPlayingBack = false;
        private float recordingStartTime;
        private int maxInputs = 100;
        private int currentPlaybackIndex = 0;
        
        public bool IsRecording => isRecording;
        public bool IsPlayingBack => isPlayingBack;
        public bool HasRecording => recordedInputs.Count > 0;
        public int RecordedInputCount => recordedInputs.Count;
        
        public void Initialize(int maxInputCount)
        {
            maxInputs = maxInputCount;
        }
        
        public void StartRecording()
        {
            recordedInputs.Clear();
            isRecording = true;
            recordingStartTime = Time.time;
            Debug.Log("[ComboRecorder] Recording started");
        }
        
        public void StopRecording()
        {
            isRecording = false;
            Debug.Log($"[ComboRecorder] Recording stopped. Captured {recordedInputs.Count} inputs");
        }
        
        public void RecordInput(string actionName, InputAction.CallbackContext context)
        {
            if (!isRecording || recordedInputs.Count >= maxInputs)
                return;
            
            RecordedInput input = new RecordedInput
            {
                actionName = actionName,
                timestamp = Time.time - recordingStartTime,
                phase = context.phase
            };
            
            recordedInputs.Add(input);
        }
        
        public void PlaybackRecording()
        {
            if (recordedInputs.Count == 0)
            {
                Debug.LogWarning("[ComboRecorder] No recorded inputs to playback");
                return;
            }
            
            isPlayingBack = true;
            currentPlaybackIndex = 0;
            StartCoroutine(PlaybackCoroutine());
        }
        
        private System.Collections.IEnumerator PlaybackCoroutine()
        {
            float playbackStartTime = Time.time;
            
            while (currentPlaybackIndex < recordedInputs.Count)
            {
                RecordedInput input = recordedInputs[currentPlaybackIndex];
                float currentTime = Time.time - playbackStartTime;
                
                if (currentTime >= input.timestamp)
                {
                    Debug.Log($"[ComboRecorder] Playback: {input.actionName} at {input.timestamp:F3}s");
                    currentPlaybackIndex++;
                }
                
                yield return null;
            }
            
            isPlayingBack = false;
            Debug.Log("[ComboRecorder] Playback complete");
        }
        
        public void ClearRecording()
        {
            recordedInputs.Clear();
            isRecording = false;
            isPlayingBack = false;
            Debug.Log("[ComboRecorder] Recording cleared");
        }
        
        public List<RecordedInput> GetRecordedInputs()
        {
            return new List<RecordedInput>(recordedInputs);
        }
        
        public void SaveRecording(string name)
        {
            ComboRecordingData data = ScriptableObject.CreateInstance<ComboRecordingData>();
            data.recordingName = name;
            data.inputs = new List<RecordedInput>(recordedInputs);
            
#if UNITY_EDITOR
            string path = $"Assets/RogueDeal/Resources/Combat/Recordings/{name}.asset";
            UnityEditor.AssetDatabase.CreateAsset(data, path);
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log($"[ComboRecorder] Recording saved to {path}");
#endif
        }
        
        public void LoadRecording(string name)
        {
            ComboRecordingData data = Resources.Load<ComboRecordingData>($"Combat/Recordings/{name}");
            if (data != null)
            {
                recordedInputs = new List<RecordedInput>(data.inputs);
                Debug.Log($"[ComboRecorder] Loaded recording '{name}' with {recordedInputs.Count} inputs");
            }
            else
            {
                Debug.LogWarning($"[ComboRecorder] Recording '{name}' not found");
            }
        }
    }
    
    [System.Serializable]
    public struct RecordedInput
    {
        public string actionName;
        public float timestamp;
        public InputActionPhase phase;
    }
}
