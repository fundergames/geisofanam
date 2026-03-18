using Funder.Core.Events;
using RogueDeal.Events;
using System.Text;
using RogueDeal.UI;
using UnityEngine;

namespace RogueDeal.Combat
{
    public class CombatFlowDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool logCombatEvents = true;
        [SerializeField] private bool logUIEvents = true;
        [SerializeField] private bool showDetailedInfo = true;
        [SerializeField] private bool logToScreen = true;
        
        private StringBuilder eventLog = new StringBuilder();
        private int eventCounter = 0;
        private Vector2 scrollPosition;
        private bool showDebugWindow = true;

        private void Start()
        {
            if (logCombatEvents)
            {
                SubscribeToCombatEvents();
            }
            
            if (logUIEvents)
            {
                SubscribeToUIEvents();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromAllEvents();
        }

        private void SubscribeToCombatEvents()
        {
            EventBus<CombatStartedEvent>.Subscribe(OnCombatStarted);
            EventBus<HandDealtEvent>.Subscribe(OnHandDealt);
            EventBus<HandEvaluatedEvent>.Subscribe(OnHandEvaluated);
            EventBus<PlayerAttackEvent>.Subscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Subscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Subscribe(OnEnemyDefeated);
            EventBus<StatusEffectAppliedEvent>.Subscribe(OnStatusEffectApplied);
            EventBus<CombatEndedEvent>.Subscribe(OnCombatEnded);
            EventBus<TurnStartEvent>.Subscribe(OnTurnStart);
        }

        private void SubscribeToUIEvents()
        {
            EventBus<DrawCardsRequestEvent>.Subscribe(OnDrawCardsRequest);
        }

        private void UnsubscribeFromAllEvents()
        {
            EventBus<CombatStartedEvent>.Unsubscribe(OnCombatStarted);
            EventBus<HandDealtEvent>.Unsubscribe(OnHandDealt);
            EventBus<HandEvaluatedEvent>.Unsubscribe(OnHandEvaluated);
            EventBus<PlayerAttackEvent>.Unsubscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Unsubscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Unsubscribe(OnEnemyDefeated);
            EventBus<StatusEffectAppliedEvent>.Unsubscribe(OnStatusEffectApplied);
            EventBus<CombatEndedEvent>.Unsubscribe(OnCombatEnded);
            EventBus<TurnStartEvent>.Unsubscribe(OnTurnStart);
            EventBus<DrawCardsRequestEvent>.Unsubscribe(OnDrawCardsRequest);
        }

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            LogEvent("COMBAT STARTED", 
                $"Stage ID: {evt.stageId}\n" +
                $"Enemy Count: {evt.enemyCount}");
        }

        private void OnHandDealt(HandDealtEvent evt)
        {
            StringBuilder cards = new StringBuilder();
            foreach (var card in evt.cards)
            {
                cards.Append($"[{GetCardString(card)}] ");
            }
            
            LogEvent("HAND DEALT", 
                $"Turn: {evt.turnNumber}\n" +
                $"Cards: {cards}");
        }

        private void OnDrawCardsRequest(DrawCardsRequestEvent evt)
        {
            StringBuilder heldInfo = new StringBuilder();
            for (int i = 0; i < evt.heldCardFlags.Count; i++)
            {
                heldInfo.Append($"Card {i}: {(evt.heldCardFlags[i] ? "HELD" : "DRAW")} ");
            }
            
            LogEvent("DRAW REQUEST", 
                $"Held Cards: {heldInfo}",
                Color.cyan);
        }

        private void OnHandEvaluated(HandEvaluatedEvent evt)
        {
            LogEvent("HAND EVALUATED", 
                $"Hand Type: {evt.handType}\n" +
                $"Base Damage: {evt.baseDamage}\n" +
                $"Critical Hit: {(evt.isCrit ? "YES!" : "No")}", 
                evt.isCrit ? Color.yellow : Color.white);
        }

        private void OnPlayerAttack(PlayerAttackEvent evt)
        {
            LogEvent("PLAYER ATTACK", 
                $"Hand: {evt.handType}\n" +
                $"Damage: {evt.damageDealt}\n" +
                $"Damage Type: {evt.damageType}\n" +
                $"Critical: {(evt.isCrit ? "YES!" : "No")}\n" +
                $"Target: {evt.target?.definition?.displayName ?? "Unknown"}",
                Color.green);
        }

        private void OnEnemyAttack(EnemyAttackEvent evt)
        {
            LogEvent("ENEMY ATTACK", 
                $"Attacker: {evt.attacker?.definition?.displayName ?? "Unknown"}\n" +
                $"Damage: {evt.damageDealt}\n" +
                $"Dodged: {(evt.dodged ? "YES!" : "No")}",
                evt.dodged ? Color.blue : Color.red);
        }

        private void OnStatusEffectApplied(StatusEffectAppliedEvent evt)
        {
            LogEvent("STATUS EFFECT", 
                $"Effect: {evt.effectType}\n" +
                $"Stacks: {evt.stacks}\n" +
                $"Target: {(evt.isPlayer ? "Player" : "Enemy")}",
                Color.magenta);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            LogEvent("ENEMY DEFEATED", 
                $"Enemy: {evt.enemy?.definition?.displayName ?? "Unknown"}\n" +
                $"Gold Dropped: {evt.goldDropped}\n" +
                $"Items Dropped: {evt.itemsDropped?.Length ?? 0}",
                Color.yellow);
        }

        private void OnTurnStart(TurnStartEvent evt)
        {
            LogEvent("TURN START", 
                $"Turn Number: {evt.turnNumber}\n" +
                $"Remaining Turns: {evt.remainingTurns}");
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            LogEvent("COMBAT ENDED", 
                $"Victory: {(evt.playerVictory ? "YES!" : "No")}\n" +
                $"Turns Used: {evt.turnsUsed}/{evt.totalTurns}\n" +
                $"Gold Earned: {evt.goldEarned}\n" +
                $"XP Earned: {evt.xpEarned}",
                evt.playerVictory ? Color.green : Color.red);
        }

        private void LogEvent(string eventName, string details, Color color = default)
        {
            eventCounter++;
            
            if (color == default)
                color = Color.white;
            
            string timestamp = Time.time.ToString("F2");
            string logMessage = $"[{eventCounter}] [{timestamp}s] {eventName}\n{details}\n";
            
            if (showDetailedInfo)
            {
                Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{logMessage}</color>");
            }
            
            if (logToScreen)
            {
                eventLog.AppendLine($"═══ [{eventCounter}] {eventName} ({timestamp}s) ═══");
                eventLog.AppendLine(details);
                eventLog.AppendLine();
            }
        }

        private string GetCardString(Cards.Card card)
        {
            if (card.isWild)
                return "WILD";
            
            string rank = card.rank.ToString();
            string suit = GetSuitSymbol(card.suit);
            return $"{rank}{suit}";
        }

        private string GetSuitSymbol(Cards.CardSuit suit)
        {
            return suit switch
            {
                Cards.CardSuit.Hearts => "♥",
                Cards.CardSuit.Diamonds => "♦",
                Cards.CardSuit.Clubs => "♣",
                Cards.CardSuit.Spades => "♠",
                _ => "?"
            };
        }

        private void OnGUI()
        {
            if (!logToScreen || !showDebugWindow)
                return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"Combat Flow Debugger (Events: {eventCounter})", 
                new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            
            if (GUILayout.Button("Clear Log"))
            {
                eventLog.Clear();
                eventCounter = 0;
            }
            
            GUILayout.Space(10);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, 
                GUILayout.Height(Screen.height - 100));
            
            GUILayout.Label(eventLog.ToString(), 
                new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true });
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                showDebugWindow = !showDebugWindow;
            }
        }
    }
}
