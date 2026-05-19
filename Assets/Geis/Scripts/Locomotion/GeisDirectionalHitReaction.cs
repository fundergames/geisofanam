/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using Geis.Animation;
using Geis.Combat;
using RogueDeal.Combat;
using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// Plays front/back/left/right hit reactions on the Geis player from combat strike direction.
    /// Add to the player with <see cref="CombatEntity"/>; wire <see cref="DirectionalHitReactionSet"/> and animator states in
    /// <c>AC_Polygon_Masculine_Geis</c> (or set <see cref="useDirectionalTriggers"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class GeisDirectionalHitReaction : MonoBehaviour, ICombatHitReactionPresenter
    {
        [SerializeField] private DirectionalHitReactionSet reactionSet;
        [SerializeField] private Animator animator;
        [SerializeField] private CombatEntity combatEntity;

        [Header("Animator parameters")]
        [Tooltip("Int 0=Front, 1=Back, 2=Left, 3=Right. Used with hitTrigger when useDirectionalTriggers is false.")]
        [SerializeField] private string hitDirectionIntParameter = "HitDirection";

        [SerializeField] private string hitTrigger = "TakeDamage";

        [Header("Playback")]
        [Tooltip("When false (default), sets HitDirection and fires TakeDamage so the HitReaction animator layer transitions. When true, CrossFades on layer 0 instead.")]
        [SerializeField] private bool crossFadeToState;

        [SerializeField] private float crossFadeDuration = 0.08f;

        [Tooltip("When true, fires per-direction triggers from the reaction set instead of HitDirection + TakeDamage.")]
        [SerializeField] private bool useDirectionalTriggers;

        [SerializeField] private bool debugLog;

        [Tooltip("Override layer on the player animator (default HitReaction). Left at default unless your controller uses another name.")]
        [SerializeField] private string hitReactionLayerName = "HitReaction";

        private int _hitReactionLayerIndex = -1;

        private void Awake()
        {
            if (combatEntity == null)
                combatEntity = GetComponent<CombatEntity>();
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            ResolveHitReactionLayerIndex();
            SuppressHitReactionLayer();
        }

        public void PresentHitReaction(CombatEventData data)
        {
            if (data == null || data.target == null)
                return;

            if ((Object)data.target.GetComponent<ICombatHitReactionPresenter>() != this)
                return;

            PlayDirectionalReaction(data);
        }

        private bool ResolveHitReactionLayerIndex()
        {
            if (animator == null || string.IsNullOrEmpty(hitReactionLayerName))
            {
                _hitReactionLayerIndex = -1;
                return false;
            }

            if (animator.runtimeAnimatorController == null)
            {
                _hitReactionLayerIndex = -1;
                return false;
            }

            int index = animator.GetLayerIndex(hitReactionLayerName);
            if (index < 0 && debugLog)
            {
                Debug.LogWarning(
                    $"[GeisDirectionalHitReaction] Animator layer '{hitReactionLayerName}' not found on '{animator.gameObject.name}'.",
                    this);
            }

            _hitReactionLayerIndex = index;
            return index >= 0;
        }

        private void SuppressHitReactionLayer()
        {
            if (!ResolveHitReactionLayerIndex())
                return;

            animator.SetLayerWeight(_hitReactionLayerIndex, 0f);
        }

        private void ActivateHitReactionLayer()
        {
            if (!ResolveHitReactionLayerIndex())
                return;

            animator.SetLayerWeight(_hitReactionLayerIndex, 1f);
        }

        private void PlayDirectionalReaction(CombatEventData data)
        {
            if (animator == null || reactionSet == null)
                return;

            CombatHitDirection direction = data.source != null
                ? CombatHitDirectionUtility.Resolve(data.source, combatEntity)
                : data.hitDirection;

            int directionInt = CombatHitDirectionUtility.ToAnimatorInt(direction);
            CombatHitDirection reactionDirection = CombatHitDirectionUtility.ToReactionDirection(direction);

            if (useDirectionalTriggers)
            {
                ActivateHitReactionLayer();
                if (TryPlayDirectionalTrigger(direction))
                {
                    if (debugLog)
                        Debug.Log($"[GeisDirectionalHitReaction] Trigger {reactionSet.GetTriggerName(direction)} ({direction})", this);
                    return;
                }
            }

            if (crossFadeToState && ResolveHitReactionLayerIndex())
            {
                ActivateHitReactionLayer();

                if (AnimatorParameterGuard.HasParameterOfType(animator, hitDirectionIntParameter, AnimatorControllerParameterType.Int))
                    animator.SetInteger(hitDirectionIntParameter, directionInt);

                string stateName = reactionSet.GetStateName(direction);
                if (!string.IsNullOrEmpty(stateName))
                {
                    int hash = Animator.StringToHash(stateName);
                    animator.CrossFadeInFixedTime(hash, crossFadeDuration, _hitReactionLayerIndex, 0f);
                    if (debugLog)
                        Debug.Log($"[GeisDirectionalHitReaction] CrossFade {stateName} on layer {_hitReactionLayerIndex} ({direction})", this);
                }

                return;
            }

            ActivateHitReactionLayer();

            if (AnimatorParameterGuard.HasParameterOfType(animator, hitDirectionIntParameter, AnimatorControllerParameterType.Int))
                animator.SetInteger(hitDirectionIntParameter, directionInt);
            else
                Debug.LogWarning(
                    $"[GeisDirectionalHitReaction] Animator on '{animator.gameObject.name}' has no int '{hitDirectionIntParameter}'. " +
                    $"Parameters: {AnimatorParameterGuard.FormatParameterList(animator)}",
                    this);

            if (AnimatorParameterGuard.TrySetTrigger(animator, hitTrigger))
            {
                if (debugLog)
                    Debug.Log(
                        $"[GeisDirectionalHitReaction] layer={_hitReactionLayerIndex} weight={animator.GetLayerWeight(_hitReactionLayerIndex):F2} " +
                        $"strike={direction} react={reactionDirection} {hitDirectionIntParameter}={directionInt}",
                        this);
            }
            else
            {
                Debug.LogWarning(
                    $"[GeisDirectionalHitReaction] Failed to set trigger '{hitTrigger}' on '{animator.gameObject.name}'. " +
                    $"Parameters: {AnimatorParameterGuard.FormatParameterList(animator)}",
                    this);
            }
        }

        private bool TryPlayDirectionalTrigger(CombatHitDirection direction)
        {
            string trigger = reactionSet.GetTriggerName(direction);
            return !string.IsNullOrEmpty(trigger) && AnimatorParameterGuard.TrySetTrigger(animator, trigger);
        }
    }
}
