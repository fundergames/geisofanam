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