using UnityEngine;

namespace RogueDeal.Combat
{
    [CreateAssetMenu(fileName = "NewAbility", menuName = "RogueDeal/Combat/Ability")]
    public class AbilityData : ScriptableObject
    {
        [Header("Basic Info")]
        public string abilityName;
        public Sprite icon;
        
        [Header("Gameplay")]
        public float cooldown;
        public float range;
        public TargetType targetType;
        
        [Header("Effects")]
        public EffectData[] effects;
        
        [Header("Visuals")]
        public GameObject vfxPrefab;
        public AudioClip sfx;
        public AnimationClip animation;
        
        [Header("Advanced Sequencing")]
        public CombatSequenceAsset sequenceAsset;
    }
}
