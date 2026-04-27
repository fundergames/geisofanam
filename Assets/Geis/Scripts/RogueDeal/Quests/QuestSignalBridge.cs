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
using Funder.Core.Services;
using RogueDeal.Events;

namespace RogueDeal.Quests
{
    /// <summary>
    /// Bridge component that listens to game events and converts them to quest signals.
    /// Attach this to a GameObject in your scene (typically near GameBootstrap).
    /// </summary>
    public class QuestSignalBridge : MonoBehaviour
    {
        private void Start()
        {
            EventBus<EnemyDefeatedEvent>.Subscribe(OnEnemyDefeated);
            EventBus<CombatEndedEvent>.Subscribe(OnCombatEnded);
        }

        private void OnDestroy()
        {
            EventBus<EnemyDefeatedEvent>.Unsubscribe(OnEnemyDefeated);
            EventBus<CombatEndedEvent>.Unsubscribe(OnCombatEnded);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            if (evt.enemy?.definition != null)
            {
                EventBus<QuestSignalEvent>.Raise(new QuestSignalEvent
                {
                    key = "enemy_defeated",
                    targetId = evt.enemy.definition.enemyId,
                    amount = 1
                });

                // Also raise a generic enemy_defeated without specific ID
                EventBus<QuestSignalEvent>.Raise(new QuestSignalEvent
                {
                    key = "enemy_defeated",
                    targetId = "",  // Generic - matches any enemy
                    amount = 1
                });
            }
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            if (evt.playerVictory)
            {
                EventBus<QuestSignalEvent>.Raise(new QuestSignalEvent
                {
                    key = "combat_completed",
                    targetId = "",
                    amount = 1
                });
            }
        }
    }
}