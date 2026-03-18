using UnityEngine;
using System.Collections.Generic;

namespace RogueDeal.Combat.Training
{
    [CreateAssetMenu(fileName = "ComboRecording", menuName = "RogueDeal/Combat/Combo Recording")]
    public class ComboRecordingData : ScriptableObject
    {
        public string recordingName;
        public List<RecordedInput> inputs = new List<RecordedInput>();
        
        [Header("Metadata")]
        public float totalDuration;
        public int hitCount;
        public string description;
    }
}
