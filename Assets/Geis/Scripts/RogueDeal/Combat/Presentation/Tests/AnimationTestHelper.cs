/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

using UnityEngine;

namespace RogueDeal.Combat.Presentation.Tests
{
    /// <summary>
    /// Helper to manually trigger animation events for testing without actual animations.
    /// Attach this to your attacker GameObject to simulate animation events.
    /// </summary>
    public class AnimationTestHelper : MonoBehaviour
    {
        [Header("Manual Testing")]
        [Tooltip("Manually trigger animation events for testing")]
        public bool enableManualTesting = true;
        
        [Header("Test Events")]
        [Tooltip("Key to trigger EnableHitbox")]
        public KeyCode enableHitboxKey = KeyCode.E;
        
        [Tooltip("Key to trigger DisableHitbox")]
        public KeyCode disableHitboxKey = KeyCode.D;
        
        [Tooltip("Key to trigger ApplyEffects")]
        public KeyCode applyEffectsKey = KeyCode.A;
        
        [Tooltip("Key to trigger ComboHit")]
        public KeyCode comboHitKey = KeyCode.C;
        
        private CombatEventReceiver eventReceiver;
        
        private void Awake()
        {
            eventReceiver = GetComponent<CombatEventReceiver>();
            if (eventReceiver == null)
            {
                eventReceiver = GetComponentInParent<CombatEventReceiver>();
            }
        }
        
        private void Update()
        {
            if (!enableManualTesting || eventReceiver == null) return;
            
            if (Input.GetKeyDown(enableHitboxKey))
            {
                eventReceiver.OnCombatEvent("EnableHitbox");
                Debug.Log("[AnimationTestHelper] Triggered EnableHitbox");
            }
            
            if (Input.GetKeyDown(disableHitboxKey))
            {
                eventReceiver.OnCombatEvent("DisableHitbox");
                Debug.Log("[AnimationTestHelper] Triggered DisableHitbox");
            }
            
            if (Input.GetKeyDown(applyEffectsKey))
            {
                eventReceiver.OnCombatEvent("ApplyEffects");
                Debug.Log("[AnimationTestHelper] Triggered ApplyEffects");
            }
            
            if (Input.GetKeyDown(comboHitKey))
            {
                eventReceiver.OnCombatEvent("ComboHit");
                Debug.Log("[AnimationTestHelper] Triggered ComboHit");
            }
        }
        
        private void OnGUI()
        {
            if (!enableManualTesting) return;
            
            GUI.Box(new Rect(10, Screen.height - 150, 300, 140), "Animation Test Controls");
            GUI.Label(new Rect(20, Screen.height - 130, 280, 20), $"[{enableHitboxKey}] Enable Hitbox");
            GUI.Label(new Rect(20, Screen.height - 110, 280, 20), $"[{disableHitboxKey}] Disable Hitbox");
            GUI.Label(new Rect(20, Screen.height - 90, 280, 20), $"[{applyEffectsKey}] Apply Effects");
            GUI.Label(new Rect(20, Screen.height - 70, 30, 20), $"[{comboHitKey}] Combo Hit");
        }
    }
}

