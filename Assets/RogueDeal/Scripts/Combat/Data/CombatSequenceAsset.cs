using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace RogueDeal.Combat
{
    [CreateAssetMenu(fileName = "NewCombatSequence", menuName = "RogueDeal/Combat/Combat Sequence")]
    public class CombatSequenceAsset : ScriptableObject
    {
        public string sequenceName;
        public TimelineAsset timeline;
        public float duration = 3f;
        
        [Header("Preview Settings")]
        public GameObject attackerPreviewPrefab;
        public GameObject targetPreviewPrefab;
    }
}
