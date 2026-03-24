using UnityEngine;
using System.Collections.Generic;

namespace RogueDeal.Combat.Training
{
    public class FrameDataAnalyzer : MonoBehaviour
    {
        [Header("Frame Data Settings")]
        [SerializeField] private bool collectFrameData = true;
        [SerializeField] private int targetFrameRate = 60;
        
        [Header("Analysis")]
        [SerializeField] private List<AbilityFrameData> abilityFrameData = new List<AbilityFrameData>();
        
        private Dictionary<string, AbilityAnalysis> activeAnalyses = new Dictionary<string, AbilityAnalysis>();
        
        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
        }
        
        private void OnEnable()
        {
            CombatEvents.OnAttackStarted += OnAttackStarted;
            CombatEvents.OnAttackConnected += OnAttackConnected;
            CombatEvents.OnAttackCompleted += OnAttackCompleted;
        }
        
        private void OnDisable()
        {
            CombatEvents.OnAttackStarted -= OnAttackStarted;
            CombatEvents.OnAttackConnected -= OnAttackConnected;
            CombatEvents.OnAttackCompleted -= OnAttackCompleted;
        }
        
        private void OnAttackStarted(CombatEventData data)
        {
            if (!collectFrameData || data.ability == null) return;
            
            string abilityKey = GetAbilityKey(data);
            
            AbilityAnalysis analysis = new AbilityAnalysis
            {
                abilityName = data.ability.abilityName,
                startFrame = Time.frameCount,
                startTime = Time.time
            };
            
            activeAnalyses[abilityKey] = analysis;
        }
        
        private void OnAttackConnected(CombatEventData data)
        {
            if (!collectFrameData || data.ability == null) return;
            
            string abilityKey = GetAbilityKey(data);
            
            if (activeAnalyses.TryGetValue(abilityKey, out AbilityAnalysis analysis))
            {
                analysis.activeFrame = Time.frameCount;
                analysis.activeTime = Time.time;
                activeAnalyses[abilityKey] = analysis;
            }
        }
        
        private void OnAttackCompleted(CombatEventData data)
        {
            if (!collectFrameData || data.ability == null) return;
            
            string abilityKey = GetAbilityKey(data);
            
            if (activeAnalyses.TryGetValue(abilityKey, out AbilityAnalysis analysis))
            {
                analysis.endFrame = Time.frameCount;
                analysis.endTime = Time.time;
                
                int startupFrames = analysis.activeFrame - analysis.startFrame;
                int activeFrames = analysis.endFrame - analysis.activeFrame;
                int totalFrames = analysis.endFrame - analysis.startFrame;
                
                float startupTime = analysis.activeTime - analysis.startTime;
                float activeTime = analysis.endTime - analysis.activeTime;
                float totalTime = analysis.endTime - analysis.startTime;
                
                AbilityFrameData frameData = new AbilityFrameData
                {
                    abilityName = analysis.abilityName,
                    startupFrames = startupFrames,
                    activeFrames = activeFrames,
                    recoveryFrames = 0,
                    totalFrames = totalFrames,
                    startupTime = startupTime,
                    activeTime = activeTime,
                    recoveryTime = 0f,
                    totalTime = totalTime
                };
                
                abilityFrameData.Add(frameData);
                activeAnalyses.Remove(abilityKey);
                
                Debug.Log($"[FrameDataAnalyzer] {frameData.abilityName}:\n" +
                         $"  Startup: {startupFrames}f ({startupTime:F3}s)\n" +
                         $"  Active: {activeFrames}f ({activeTime:F3}s)\n" +
                         $"  Total: {totalFrames}f ({totalTime:F3}s)");
            }
        }
        
        private string GetAbilityKey(CombatEventData data)
        {
            return $"{data.source.gameObject.GetInstanceID()}_{data.ability.abilityName}";
        }
        
        public AbilityFrameData GetFrameData(string abilityName)
        {
            foreach (var data in abilityFrameData)
            {
                if (data.abilityName == abilityName)
                {
                    return data;
                }
            }
            
            return default;
        }
        
        public List<AbilityFrameData> GetAllFrameData()
        {
            return new List<AbilityFrameData>(abilityFrameData);
        }
        
        public void ClearFrameData()
        {
            abilityFrameData.Clear();
            activeAnalyses.Clear();
        }
        
        public void ExportFrameData(string filePath)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Ability Name,Startup Frames,Active Frames,Recovery Frames,Total Frames,Startup Time,Active Time,Recovery Time,Total Time");
            
            foreach (var data in abilityFrameData)
            {
                sb.AppendLine($"{data.abilityName},{data.startupFrames},{data.activeFrames},{data.recoveryFrames},{data.totalFrames}," +
                            $"{data.startupTime:F4},{data.activeTime:F4},{data.recoveryTime:F4},{data.totalTime:F4}");
            }
            
            System.IO.File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"[FrameDataAnalyzer] Frame data exported to {filePath}");
        }
        
        private struct AbilityAnalysis
        {
            public string abilityName;
            public int startFrame;
            public int activeFrame;
            public int endFrame;
            public float startTime;
            public float activeTime;
            public float endTime;
        }
    }
    
    [System.Serializable]
    public struct AbilityFrameData
    {
        public string abilityName;
        public int startupFrames;
        public int activeFrames;
        public int recoveryFrames;
        public int totalFrames;
        public float startupTime;
        public float activeTime;
        public float recoveryTime;
        public float totalTime;
    }
}
