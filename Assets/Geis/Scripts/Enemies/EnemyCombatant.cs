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

using System.Collections;
using Funder.Core.Events;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Events;
using RogueDeal.Items;
using UnityEngine;

namespace Geis.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatEntity))]
    public class EnemyCombatant : MonoBehaviour
    {
        [SerializeField] private EnemyAiDefinition definition;
        [SerializeField] private EnemyVisual enemyVisual;
        [SerializeField] private EnemyBrain brain;
        [SerializeField] private EnemyPerception perception;
        [SerializeField] private EnemyMotor motor;
        [SerializeField] private EnemyAttackDriver attackDriver;
        [SerializeField] private EnemyAnimatorDriver animatorDriver;
        [SerializeField] private EnemyCoordinationContext coordinationContext;
        [SerializeField] private EnemyWeaponEquipper weaponEquipper;
        [SerializeField] private CombatEntity combatEntity;

        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _isDefeated;
        private Coroutine _disableRoutine;

        public EnemyAiDefinition Definition => definition;
        public CombatEntity CombatEntity => combatEntity;
        public bool IsDefeated => _isDefeated;

        private void Awake()
        {
            combatEntity = combatEntity != null ? combatEntity : GetComponent<CombatEntity>();
            enemyVisual = enemyVisual != null ? enemyVisual : GetComponent<EnemyVisual>() ?? GetComponentInChildren<EnemyVisual>();
            brain = brain != null ? brain : GetComponent<EnemyBrain>() ?? GetComponentInChildren<EnemyBrain>();
            perception = perception != null ? perception : GetComponent<EnemyPerception>() ?? GetComponentInChildren<EnemyPerception>();
            motor = motor != null ? motor : GetComponent<EnemyMotor>() ?? GetComponentInChildren<EnemyMotor>();
            attackDriver = attackDriver != null ? attackDriver : GetComponent<EnemyAttackDriver>() ?? GetComponentInChildren<EnemyAttackDriver>();
            animatorDriver = animatorDriver != null ? animatorDriver : GetComponent<EnemyAnimatorDriver>() ?? GetComponentInChildren<EnemyAnimatorDriver>();
            coordinationContext = coordinationContext != null ? coordinationContext : GetComponent<EnemyCoordinationContext>() ?? GetComponentInChildren<EnemyCoordinationContext>();
            weaponEquipper = weaponEquipper != null ? weaponEquipper : GetComponent<EnemyWeaponEquipper>() ?? GetComponentInChildren<EnemyWeaponEquipper>();

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;

            // Player SimpleAttackHitDetector commonly masks only the "Enemy" layer; Default-layer colliders are invisible to melee probes.
            EnsureEnemyLayerForMeleeHits(gameObject);
        }

        private static void EnsureEnemyLayerForMeleeHits(GameObject root)
        {
            int layer = LayerMask.NameToLayer("Enemy");
            if (layer < 0)
                return;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        private void Start()
        {
            ApplyDefinitionAndResetState();
        }

        private void Update()
        {
            if (_isDefeated || combatEntity == null)
                return;

            CombatEntityData data = combatEntity.GetEntityData();
            if (data != null && !data.IsAlive)
                HandleDefeated();
        }

        public void ApplyDefinitionAndResetState()
        {
            if (definition == null || combatEntity == null)
                return;

            gameObject.name = string.IsNullOrWhiteSpace(definition.displayName) ? gameObject.name : definition.displayName;
            combatEntity.RefreshStatsWithoutHeroData(definition.maxHealth, definition.attack, definition.defense);

            CombatEntityData data = combatEntity.GetEntityData();
            if (data != null)
            {
                data.combatProfile = definition.combatProfile;
                data.equippedWeapon = definition.GetEffectiveWeaponStats();
                data.position = transform.position;
                data.originPosition = _spawnPosition;
            }

            coordinationContext?.ApplyDefinition(definition);
            animatorDriver?.ApplyAnimatorOverrideFromDefinition(definition);
            animatorDriver?.SyncAnimatorWeaponSlotFromDefinition(definition);
            weaponEquipper?.ApplyFromDefinition(definition, combatEntity);
            motor?.ConfigureAgentFromDefinition();
            enemyVisual?.UpdateVisuals();
            ResetCombatant();
        }

        public void ResetCombatant()
        {
            if (definition == null || combatEntity == null)
                return;

            if (_disableRoutine != null)
            {
                StopCoroutine(_disableRoutine);
                _disableRoutine = null;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            _isDefeated = false;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            motor?.WarpTo(_spawnPosition);
            combatEntity.RefreshStatsWithoutHeroData(definition.maxHealth, definition.attack, definition.defense);

            CombatEntityData data = combatEntity.GetEntityData();
            if (data != null)
            {
                data.combatProfile = definition.combatProfile;
                data.equippedWeapon = definition.GetEffectiveWeaponStats();
                data.position = transform.position;
                data.originPosition = _spawnPosition;
            }

            enemyVisual?.ResetPresentation();
            enemyVisual?.UpdateVisuals();
            motor?.ConfigureAgentFromDefinition();
            animatorDriver?.ApplyAnimatorOverrideFromDefinition(definition);
            animatorDriver?.SyncAnimatorWeaponSlotFromDefinition(definition);
            weaponEquipper?.ApplyFromDefinition(definition, combatEntity);
            coordinationContext?.ResetContext();
            perception?.ResetPerception();
            attackDriver?.ResetCombatState();
            brain?.ResetBrain();
        }

        public void HandleDefeated()
        {
            if (_isDefeated)
                return;

            _isDefeated = true;
            brain?.HandleDefeated();
            enemyVisual?.AnimateDefeat();
            PublishLegacyDefeatEventIfNeeded();

            if (definition != null && definition.reactions.deathDisableDelay > 0f)
                _disableRoutine = StartCoroutine(DisableAfterDelay(definition.reactions.deathDisableDelay));
        }

        private IEnumerator DisableAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (gameObject != null)
                gameObject.SetActive(false);

            _disableRoutine = null;
        }

        private void PublishLegacyDefeatEventIfNeeded()
        {
            if (definition == null || !definition.publishLegacyDefeatEvent || definition.legacyEnemyDefinition == null)
                return;

            var legacyEnemy = new RogueDeal.Enemies.EnemyInstance(
                definition.legacyEnemyDefinition,
                Mathf.Max(1, definition.legacyWorldLevel),
                _spawnPosition);
            legacyEnemy.stats.currentHealth = 0;
            legacyEnemy.isDefeated = true;

            EventBus<EnemyDefeatedEvent>.Raise(new EnemyDefeatedEvent
            {
                enemy = legacyEnemy,
                goldDropped = definition.legacyEnemyDefinition.GetScaledGold(definition.legacyWorldLevel),
                itemsDropped = new BaseItem[0]
            });
        }
    }
}
