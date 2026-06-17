/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using RogueDeal.Combat;
using UnityEngine;

namespace Geis.Enemies
{
    public class EnemyBrain : MonoBehaviour
    {
        public enum EnemyState
        {
            Idle = 0,
            Acquire = 1,
            Approach = 2,
            Strafe = 3,
            Telegraph = 4,
            Attack = 5,
            Recover = 6,
            Stagger = 7,
            Dead = 8
        }

        [SerializeField] private bool autoRun = true;

        private EnemyCombatant _combatant;
        private EnemyPerception _perception;
        private EnemyMotor _motor;
        private EnemyAttackDriver _attackDriver;
        private EnemyAnimatorDriver _animatorDriver;
        private CombatEntity _combatEntity;

        private readonly EnemyBehaviorContext _context = new EnemyBehaviorContext();
        private EnemyBehavior[] _activePipeline;
        private EnemyAiDefinition _cachedPipelineDefinition;
        private EnemyBehavior[] _cachedPipelineSource;

        private float _staggerRemaining;
        private float _strafeDirectionUntil;
        private int _strafeDirection = 1;

        public EnemyState CurrentState { get; private set; } = EnemyState.Idle;
        public int StrafeDirection => _strafeDirection;

        private void Awake()
        {
            _combatant = GetComponent<EnemyCombatant>() ?? GetComponentInParent<EnemyCombatant>();
            _perception = GetComponent<EnemyPerception>() ?? GetComponentInParent<EnemyPerception>();
            _motor = GetComponent<EnemyMotor>() ?? GetComponentInParent<EnemyMotor>();
            _attackDriver = GetComponent<EnemyAttackDriver>() ?? GetComponentInParent<EnemyAttackDriver>();
            _animatorDriver = GetComponent<EnemyAnimatorDriver>() ?? GetComponentInParent<EnemyAnimatorDriver>();
            _combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();

            _context.Bind(this, _combatant, _perception, _motor, _attackDriver, _animatorDriver, _combatEntity);
            RebuildPipeline();
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += HandleDamageApplied;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= HandleDamageApplied;
        }

        private void Update()
        {
            if (!autoRun || _combatant == null || _combatant.Definition == null)
                return;

            RebuildPipelineIfDefinitionChanged();

            _perception?.RefreshTarget();
            _context.RefreshTargetData();
            _context.StaggerRemaining = _staggerRemaining;
            _context.StrafeDirection = _strafeDirection;
            _context.StrafeDirectionUntil = _strafeDirectionUntil;
            _context.IsStrafingThisTick = false;

            if (_context.Target != null)
                _context.FaceTarget();

            if (_activePipeline == null || _activePipeline.Length == 0)
            {
                TickIdleFallback();
                return;
            }

            for (int i = 0; i < _activePipeline.Length; i++)
            {
                EnemyBehavior step = _activePipeline[i];
                if (step == null || !step.Enabled)
                    continue;

                if (step.TryExecute(_context))
                {
                    _staggerRemaining = _context.StaggerRemaining;
                    _strafeDirection = _context.StrafeDirection;
                    _strafeDirectionUntil = _context.StrafeDirectionUntil;
                    return;
                }
            }

            TickIdleFallback();
        }

        public void EnterState(EnemyState nextState)
        {
            CurrentState = nextState;
        }

        public void UpdateStrafeDirection()
        {
            if (_combatant == null || _combatant.Definition == null)
                return;

            if (Time.time < _strafeDirectionUntil)
                return;

            _strafeDirection *= -1;
            _strafeDirectionUntil = Time.time + Mathf.Max(0.25f, _combatant.Definition.movement.strafeRepathInterval);
            _context.StrafeDirection = _strafeDirection;
            _context.StrafeDirectionUntil = _strafeDirectionUntil;
        }

        public void ResetBrain()
        {
            CurrentState = EnemyState.Idle;
            _staggerRemaining = 0f;
            _strafeDirection = 1;
            _strafeDirectionUntil = 0f;
            RebuildPipeline();
        }

        public void HandleDefeated()
        {
            EnterState(EnemyState.Dead);
            _motor?.StopMovement();
            _attackDriver?.CancelActiveAttack();
            _context.PresentLocomotion(hasTarget: false);
        }

        private void HandleDamageApplied(CombatEventData data)
        {
            if (_combatEntity == null || _combatant == null || _combatant.Definition == null)
                return;

            if (data.target != _combatEntity || data.wasImmune || data.damageAmount <= 0f || _combatant.IsDefeated)
                return;

            _staggerRemaining = _combatant.Definition.reactions.staggerDurationOnHit;
            _motor?.StopMovement();

            if (_combatEntity.GetComponent<ICombatHitReactionPresenter>() == null)
                _animatorDriver?.TriggerHitReaction();
        }

        private void RebuildPipelineIfDefinitionChanged()
        {
            EnemyAiDefinition definition = _combatant != null ? _combatant.Definition : null;
            EnemyBehavior[] source = ResolvePipelineSource(definition);
            if (ReferenceEquals(definition, _cachedPipelineDefinition) && ReferenceEquals(source, _cachedPipelineSource))
                return;

            _cachedPipelineDefinition = definition;
            _cachedPipelineSource = source;
            _activePipeline = source;
        }

        private void RebuildPipeline()
        {
            EnemyAiDefinition definition = _combatant != null ? _combatant.Definition : null;
            _cachedPipelineDefinition = definition;
            _cachedPipelineSource = ResolvePipelineSource(definition);
            _activePipeline = _cachedPipelineSource;
        }

        private static EnemyBehavior[] ResolvePipelineSource(EnemyAiDefinition definition)
        {
            if (definition == null)
                return EnemyBuiltinBehaviorPipeline.GetOrCreate();

            EnemyBehavior[] authored = definition.behaviorPipeline;
            return authored != null && authored.Length > 0
                ? authored
                : EnemyBuiltinBehaviorPipeline.GetOrCreate();
        }

        private void TickIdleFallback()
        {
            EnterState(EnemyState.Idle);
            _motor?.StopMovement();
            _context.PresentLocomotion(hasTarget: _context.Target != null);
        }
    }
}
