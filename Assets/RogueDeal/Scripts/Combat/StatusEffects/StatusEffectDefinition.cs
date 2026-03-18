using UnityEngine;

namespace RogueDeal.Combat.StatusEffects
{
    [CreateAssetMenu(fileName = "StatusEffect_", menuName = "Funder Games/Rogue Deal/Combat/Status Effect Definition")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [Header("Basic Info")]
        public StatusEffectType effectType;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;
        
        [Header("Effect Properties")]
        public int baseDamagePerStack = 5;
        public float baseStackDuration = 3f;
        public int maxStacks = 10;
        public bool canStack = true;
        public ElementalType associatedElement = ElementalType.None;
        
        [Header("Application")]
        [Tooltip("Chance to apply this effect (0-1)")]
        [Range(0f, 1f)]
        public float applicationChance = 0.3f;
        
        [Tooltip("Number of stacks applied on hit")]
        public int stacksPerApplication = 1;
        
        [Header("Visual")]
        public Color effectColor = Color.white;
        public GameObject vfxPrefab;

        public StatusEffect CreateEffect(int stackOverride = -1)
        {
            int stacks = stackOverride > 0 ? stackOverride : stacksPerApplication;
            return new StatusEffect(effectType, stacks, baseDamagePerStack, baseStackDuration)
            {
                element = associatedElement
            };
        }
    }
}
