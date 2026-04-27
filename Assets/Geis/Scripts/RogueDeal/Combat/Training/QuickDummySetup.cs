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

namespace RogueDeal.Combat.Training
{
    public class QuickDummySetup : MonoBehaviour
    {
        [ContextMenu("Setup as Training Dummy")]
        public void SetupAsTrainingDummy()
        {
            CombatEntity entity = GetComponent<CombatEntity>();
            if (entity == null)
            {
                entity = gameObject.AddComponent<CombatEntity>();
                Debug.Log($"Added CombatEntity to {gameObject.name}");
            }
            
            TrainingDummy dummy = GetComponent<TrainingDummy>();
            if (dummy == null)
            {
                dummy = gameObject.AddComponent<TrainingDummy>();
                Debug.Log($"Added TrainingDummy to {gameObject.name}");
            }
            
            entity.InitializeStatsWithoutHeroData(1000f, 10f, 5f);
            
            Debug.Log($"✓ {gameObject.name} is now a Training Dummy!");
        }
    }
}
