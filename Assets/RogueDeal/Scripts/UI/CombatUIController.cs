using Funder.Core.Events;
using RogueDeal.Events;
using RogueDeal.Combat.Cards;
using RogueDeal.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RogueDeal.UI
{
    public class CombatUIController : MonoBehaviour
    {
        [Header("Card Hand")]
        [SerializeField] private CardHandUI cardHandUI;
        
        [Header("Combat UI")]
        [SerializeField] private Button drawButton;
        [SerializeField] private TextMeshProUGUI handResultText;
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI turnCounterText;
        
        [Header("Character References")]
        [SerializeField] private PlayerVisual playerVisual;
        
        [Header("Animation Triggers")]
        [SerializeField] private string attackTriggerName = "Attack";
        [SerializeField] private string damageTriggerName = "Damage";
        [SerializeField] private string victoryTriggerName = "Victory";
        [SerializeField] private string defeatTriggerName = "Defeat";

        private void Start()
        {
            SubscribeToEvents();
            
            if (playerVisual == null)
                playerVisual = FindFirstObjectByType<PlayerVisual>();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            EventBus<CombatStartedEvent>.Subscribe(OnCombatStarted);
            EventBus<HandDealtEvent>.Subscribe(OnHandDealt);
            EventBus<HandEvaluatedEvent>.Subscribe(OnHandEvaluated);
            EventBus<PlayerAttackEvent>.Subscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Subscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Subscribe(OnEnemyDefeated);
            EventBus<CombatEndedEvent>.Subscribe(OnCombatEnded);
            EventBus<TurnStartEvent>.Subscribe(OnTurnStart);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus<CombatStartedEvent>.Unsubscribe(OnCombatStarted);
            EventBus<HandDealtEvent>.Unsubscribe(OnHandDealt);
            EventBus<HandEvaluatedEvent>.Unsubscribe(OnHandEvaluated);
            EventBus<PlayerAttackEvent>.Unsubscribe(OnPlayerAttack);
            EventBus<EnemyAttackEvent>.Unsubscribe(OnEnemyAttack);
            EventBus<EnemyDefeatedEvent>.Unsubscribe(OnEnemyDefeated);
            EventBus<CombatEndedEvent>.Unsubscribe(OnCombatEnded);
            EventBus<TurnStartEvent>.Unsubscribe(OnTurnStart);
        }

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            SetDrawButtonEnabled(false);
            
            if (handResultText != null)
                handResultText.text = "";
                
            if (damageText != null)
                damageText.text = "";
        }

        private void OnHandDealt(HandDealtEvent evt)
        {
            StartCoroutine(OnHandDealtRoutine(evt));
        }

        private IEnumerator OnHandDealtRoutine(HandDealtEvent evt)
        {
            yield return new WaitUntil(() => !cardHandUI.IsAnimating);
            
            SetDrawButtonEnabled(true);
            
            if (handResultText != null)
                handResultText.text = "Select cards to hold, then draw";
        }

        private void OnHandEvaluated(HandEvaluatedEvent evt)
        {
            StartCoroutine(OnHandEvaluatedRoutine(evt));
        }

        private IEnumerator OnHandEvaluatedRoutine(HandEvaluatedEvent evt)
        {
            yield return new WaitUntil(() => !cardHandUI.IsAnimating);
            
            if (handResultText != null)
            {
                string critText = evt.isCrit ? " CRITICAL!" : "";
                handResultText.text = $"{evt.handType}{critText}";
            }
            
            if (damageText != null)
            {
                damageText.text = $"{evt.baseDamage} damage";
            }
            
            yield return new WaitForSeconds(1.5f);
        }

        private void OnPlayerAttack(PlayerAttackEvent evt)
        {
            StartCoroutine(PlayerAttackRoutine(evt));
        }

        private IEnumerator PlayerAttackRoutine(PlayerAttackEvent evt)
        {
            cardHandUI.SetCardsInteractable(false);
            
            if (playerVisual != null && playerVisual.Animator != null)
            {
                playerVisual.Animator.SetTrigger(attackTriggerName);
                
                yield return new WaitForSeconds(0.5f);
            }
            
            yield return new WaitForSeconds(0.5f);
        }

        private void OnEnemyAttack(EnemyAttackEvent evt)
        {
            StartCoroutine(EnemyAttackRoutine(evt));
        }

        private IEnumerator EnemyAttackRoutine(EnemyAttackEvent evt)
        {
            yield return new WaitForSeconds(0.5f);
            
            if (!evt.dodged && playerVisual != null && playerVisual.Animator != null)
            {
                playerVisual.Animator.SetTrigger(damageTriggerName);
            }
            
            yield return new WaitForSeconds(0.5f);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            SetDrawButtonEnabled(false);
            cardHandUI.SetCardsInteractable(false);
            
            if (evt.playerVictory)
            {
                if (playerVisual != null && playerVisual.Animator != null)
                    playerVisual.Animator.SetTrigger(victoryTriggerName);
                    
                if (handResultText != null)
                    handResultText.text = "VICTORY!";
            }
            else
            {
                if (playerVisual != null && playerVisual.Animator != null)
                    playerVisual.Animator.SetTrigger(defeatTriggerName);
                    
                if (handResultText != null)
                    handResultText.text = "DEFEAT";
            }
        }

        private void OnTurnStart(TurnStartEvent evt)
        {
            if (turnCounterText != null)
            {
                turnCounterText.text = $"Turn {evt.turnNumber} / {evt.turnNumber + evt.remainingTurns}";
            }
        }

        private void SetDrawButtonEnabled(bool enabled)
        {
            if (drawButton != null)
            {
                drawButton.interactable = enabled;
            }
        }
    }

    public struct DrawCardsRequestEvent : IEvent
    {
        public List<bool> heldCardFlags;
    }
}
