using Funder.Core.Events;
using RogueDeal.Combat.Cards;
using RogueDeal.Events;
using RogueDeal.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace RogueDeal.Combat
{
    public class CombatFlowController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CardHandUI cardHandUI;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI buttonText;

        [Header("Character References")]
        [SerializeField] private PlayerVisual playerVisual;
        [SerializeField] private Transform enemyContainer;

        [Header("Timing Settings")]
        [SerializeField] private float discardCardDelay = 0.1f;
        [SerializeField] private float highlightDuration = 1f;
        [SerializeField] private float attackMoveSpeed = 5f;
        [SerializeField] private float attackReturnSpeed = 3f;
        [SerializeField] private float delayBeforeEnemyAttack = 0.5f;
        [SerializeField] private float delayAfterEnemyAttack = 0.5f;
        [SerializeField] private float delayBetweenHits = 0.25f;

        [Header("Position Settings")]
        [SerializeField] private Vector3 attackPositionOffset = new Vector3(-2f, 0f, 0f);

        private CombatManager combatManager;
        private EnemyVisual currentEnemyVisual;
        private Vector3 playerStartPosition;
        private bool waitingForDraw = false;
        private bool isAnimating = false;

        private const string DEAL_AGAIN_TEXT = "Deal Again";
        private const string DRAW_TEXT = "Draw";

        private void Start()
        {
            SubscribeToEvents();

            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionButtonClicked);

            if (playerVisual != null)
                playerStartPosition = playerVisual.transform.position;

            SetButtonState(DRAW_TEXT, false);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (actionButton != null)
                actionButton.onClick.RemoveListener(OnActionButtonClicked);
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
        }

        public void SetCombatManager(CombatManager manager)
        {
            combatManager = manager;
        }

        private void OnActionButtonClicked()
        {
            if (isAnimating)
                return;

            if (waitingForDraw)
            {
                StartCoroutine(DrawSequence());
            }
            else
            {
                StartCoroutine(DealAgainSequence());
            }
        }

        private IEnumerator DrawSequence()
        {
            isAnimating = true;
            waitingForDraw = false;
            SetButtonState(DRAW_TEXT, false);
            cardHandUI.SetCardsInteractable(false);

            var heldFlags = cardHandUI.GetHeldCardFlags();

            List<int> unheldIndices = new List<int>();
            for (int i = 0; i < heldFlags.Count; i++)
            {
                if (!heldFlags[i])
                    unheldIndices.Add(i);
            }

            yield return DiscardCardsSequence(unheldIndices);

            EventBus<DrawCardsRequestEvent>.Raise(new DrawCardsRequestEvent
            {
                heldCardFlags = heldFlags
            });

            isAnimating = false;
        }

        private IEnumerator DealAgainSequence()
        {
            isAnimating = true;
            SetButtonState(DEAL_AGAIN_TEXT, false);

            yield return ClearHandSequence();

            combatManager.DealNewHand();

            isAnimating = false;
        }

        private IEnumerator DiscardCardsSequence(List<int> unheldIndices)
        {
            var cards = cardHandUI.GetComponentsInChildren<CardVisual>().ToList();

            Sequence discardSequence = DOTween.Sequence();

            for (int i = 0; i < unheldIndices.Count; i++)
            {
                int index = unheldIndices[i];
                if (index < cards.Count)
                {
                    CardVisual card = cards[index];
                    Vector3 discardPos = new Vector3(-800f, 400f, 0f);
                    
                    discardSequence.Insert(
                        i * discardCardDelay,
                        card.AnimateDiscard(discardPos, 0.4f)
                    );
                }
            }

            var heldCards = cardHandUI.GetComponentsInChildren<CardVisual>().ToList();
            for (int i = 0; i < heldCards.Count; i++)
            {
                if (heldCards[i].IsHeld)
                {
                    heldCards[i].SetHeld(false);
                }
            }

            yield return discardSequence.WaitForCompletion();
        }

        private void OnCombatStarted(CombatStartedEvent evt)
        {
            waitingForDraw = false;
            SetButtonState(DRAW_TEXT, false);

            if (enemyContainer != null)
            {
                currentEnemyVisual = enemyContainer.GetComponentInChildren<EnemyVisual>();
            }
        }

        private void OnHandDealt(HandDealtEvent evt)
        {
            StartCoroutine(HandDealtSequence());
        }

        private IEnumerator HandDealtSequence()
        {
            yield return new WaitUntil(() => !cardHandUI.IsAnimating);

            waitingForDraw = true;
            SetButtonState(DRAW_TEXT, true);
        }

        private void OnHandEvaluated(HandEvaluatedEvent evt)
        {
            StartCoroutine(HandEvaluatedSequence(evt));
        }

        private IEnumerator HandEvaluatedSequence(HandEvaluatedEvent evt)
        {
            yield return new WaitUntil(() => !cardHandUI.IsAnimating);

            yield return HighlightBestHandSequence(evt.handType);

            yield return new WaitForSeconds(highlightDuration);

            yield return PlayerJumpAndAttackSequence();
        }

        private IEnumerator HighlightBestHandSequence(PokerHandType handType)
        {
            List<Card> currentHand = combatManager.CurrentHand;
            yield return cardHandUI.HighlightWinningCards(handType, currentHand);
        }

        private IEnumerator PlayerJumpAndAttackSequence()
        {
            if (playerVisual == null || currentEnemyVisual == null)
                yield break;

            Vector3 attackPosition = currentEnemyVisual.transform.position + attackPositionOffset;

            float moveTime = Vector3.Distance(playerVisual.transform.position, attackPosition) / attackMoveSpeed;
            yield return playerVisual.transform.DOMove(attackPosition, moveTime).SetEase(Ease.OutCubic).WaitForCompletion();

            if (playerVisual.Animator != null)
                playerVisual.Animator.SetTrigger("Attack_1");

            yield return new WaitForSeconds(0.3f);
        }

        private void OnPlayerAttack(PlayerAttackEvent evt)
        {
            StartCoroutine(PlayerAttackReactionSequence(evt));
        }

        private IEnumerator PlayerAttackReactionSequence(PlayerAttackEvent evt)
        {
            if (evt.hitNumber > 1)
            {
                yield return new WaitForSeconds(delayBetweenHits);
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (currentEnemyVisual != null)
            {
                if (evt.target.isDefeated)
                {
                    if (currentEnemyVisual.Animator != null)
                        currentEnemyVisual.Animator.SetTrigger("Die");
                    
                    yield return currentEnemyVisual.AnimateDamage(evt.damageDealt, evt.isCrit).WaitForCompletion();
                    yield return currentEnemyVisual.AnimateDefeat().WaitForCompletion();
                }
                else
                {
                    if (currentEnemyVisual.Animator != null)
                        currentEnemyVisual.Animator.SetTrigger("Damage");
                    
                    yield return currentEnemyVisual.AnimateDamage(evt.damageDealt, evt.isCrit).WaitForCompletion();
                }
            }
        }

        private void OnEnemyAttack(EnemyAttackEvent evt)
        {
            StartCoroutine(EnemyAttackSequence(evt));
        }

        private IEnumerator EnemyAttackSequence(EnemyAttackEvent evt)
        {
            yield return new WaitForSeconds(delayBeforeEnemyAttack);

            if (currentEnemyVisual != null && currentEnemyVisual.Animator != null)
                currentEnemyVisual.Animator.SetTrigger("Attack_1");

            yield return new WaitForSeconds(0.3f);

            if (evt.dodged)
            {
                if (playerVisual != null && playerVisual.Animator != null)
                    playerVisual.Animator.SetTrigger("Dodge");
            }
            else
            {
                if (playerVisual != null)
                {
                    if (playerVisual.Animator != null)
                        playerVisual.Animator.SetTrigger("Damage");
                    
                    yield return playerVisual.AnimateDamage(evt.damageDealt).WaitForCompletion();
                }
            }

            yield return new WaitForSeconds(delayAfterEnemyAttack);

            yield return PlayerReturnSequence();

            SetButtonState(DEAL_AGAIN_TEXT, true);
            waitingForDraw = false;
        }

        private IEnumerator PlayerReturnSequence()
        {
            if (playerVisual == null)
                yield break;

            float returnTime = Vector3.Distance(playerVisual.transform.position, playerStartPosition) / attackReturnSpeed;
            yield return playerVisual.transform.DOMove(playerStartPosition, returnTime).SetEase(Ease.InOutCubic).WaitForCompletion();
        }

        private IEnumerator ClearHandSequence()
        {
            var cards = cardHandUI.GetComponentsInChildren<CardVisual>();

            Sequence clearSequence = DOTween.Sequence();
            for (int i = 0; i < cards.Length; i++)
            {
                Vector3 discardPos = new Vector3(-800f, 400f, 0f);
                clearSequence.Insert(
                    i * discardCardDelay,
                    cards[i].AnimateDiscard(discardPos, 0.4f)
                );
            }

            yield return clearSequence.WaitForCompletion();

            cardHandUI.ClearHand();
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent evt)
        {
            StartCoroutine(EnemyDefeatedSequence());
        }

        private IEnumerator EnemyDefeatedSequence()
        {
            yield return PlayerReturnSequence();

            SetButtonState(DEAL_AGAIN_TEXT, true);
            waitingForDraw = false;
        }

        private void OnCombatEnded(CombatEndedEvent evt)
        {
            SetButtonState(DEAL_AGAIN_TEXT, false);
        }

        private void SetButtonState(string text, bool interactable)
        {
            if (buttonText != null)
                buttonText.text = text;

            if (actionButton != null)
                actionButton.interactable = interactable;
        }
    }
}
