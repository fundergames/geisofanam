using System;
using UnityEngine;

namespace RogueDeal.Combat
{
    [Serializable]
    public class ClassAttackMapping
    {
        [Header("Attack Properties")]
        public string attackName;
        [TextArea(2, 3)]
        public string attackDescription;
        public int numberOfHits = 1;
        public float timeBetweenHits = 0.3f;
        public bool isAOE = false;
        
        [Header("Damage Modifiers")]
        public float damageMultiplier = 1f;
        public DamageType damageType = DamageType.Physical;
        
        [Header("Visual/Audio")]
        public GameObject attackVFXPrefab;
        public AudioClip attackSound;
        public string animationTrigger;
        
        [Header("Camera Effects")]
        public bool enableScreenShake = false;
        public float screenShakeIntensity = 0.2f;
        public float screenShakeDuration = 0.3f;
    }
}
