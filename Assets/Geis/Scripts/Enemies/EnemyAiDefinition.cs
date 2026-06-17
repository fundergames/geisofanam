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

using Geis.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Enemies;
using UnityEngine;

namespace Geis.Enemies
{
    [CreateAssetMenu(fileName = "EnemyAI_", menuName = "Funder Games/Geis/Enemies/Enemy AI Definition")]
    public class EnemyAiDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string enemyId = "enemy_phase1_humanoid";
        public string displayName = "Phase 1 Humanoid";
        [TextArea(2, 4)] public string description;

        [Header("Runtime Combat")]
        public float maxHealth = 120f;
        public float attack = 14f;
        public float defense = 4f;

        [Tooltip("Preferred loadout (prefab + GeisComboData + RogueDeal Weapon/CombatAction). Matches the player GeisWeaponSwitcher pattern. When set, overrides equippedWeapon for stats and drives combo-resolved attacks.")]
        public GeisWeaponDefinition weaponDefinition;

        [Tooltip("Animator EquippedWeaponIndex when using Polygon/player controllers (0=Unarmed, 1=Knife, 2=Sword, 3=Bow). Ignored when weaponDefinition is null.")]
        [Range(0, 3)]
        public int animatorEquippedWeaponSlotIndex = 2;

        [Tooltip("Fallback RogueDeal weapon when weaponDefinition is null.")]
        public Weapon equippedWeapon;

        public CombatProfile combatProfile;
        public RuntimeAnimatorController animatorOverrideController;

        [Header("Perception")]
        public EnemyPerceptionSettings perception = new EnemyPerceptionSettings();

        [Header("Movement")]
        public EnemyMovementSettings movement = new EnemyMovementSettings();

        [Header("Reactions")]
        public EnemyReactionSettings reactions = new EnemyReactionSettings();

        [Header("Behavior Pipeline")]
        [Tooltip("Ordered list — first behavior that handles the tick wins. Leave empty to use the built-in melee pipeline (dead → stagger → attack phase → acquire → melee attack → strafe → approach).")]
        public EnemyBehavior[] behaviorPipeline = new EnemyBehavior[0];

        [Header("Attack Loadout")]
        public EnemyAttackDefinition[] attacks = new EnemyAttackDefinition[0];

        [Header("Future Coordination Hooks")]
        public string defaultSquadId;
        public EnemyCombatRole defaultCombatRole = EnemyCombatRole.Frontliner;

        [Header("Optional Legacy Progression Bridge")]
        public EnemyDefinition legacyEnemyDefinition;
        [Min(1)] public int legacyWorldLevel = 1;
        public bool publishLegacyDefeatEvent = false;

        /// <summary>RogueDeal weapon for damage scaling: weaponDefinition.weaponStats when set, else equippedWeapon.</summary>
        public Weapon GetEffectiveWeaponStats()
        {
            if (weaponDefinition != null)
                return weaponDefinition.GetWeaponForDamage();
            return equippedWeapon;
        }

        public float GetPreferredCombatDistance()
        {
            if (movement.preferredDistance > 0f)
                return movement.preferredDistance;

            if (attacks == null || attacks.Length == 0)
                return combatProfile != null ? combatProfile.engagementDistance : 2f;

            float total = 0f;
            int count = 0;
            for (int i = 0; i < attacks.Length; i++)
            {
                var attackDef = attacks[i];
                if (attackDef == null || attackDef.maxRange <= 0f)
                    continue;

                total += Mathf.Max(attackDef.minRange, attackDef.maxRange * 0.75f);
                count++;
            }

            return count > 0 ? total / count : (combatProfile != null ? combatProfile.engagementDistance : 2f);
        }

        /// <summary>
        /// Largest <see cref="EnemyAttackDefinition.maxRange"/> in <see cref="attacks"/> — used to decide when the enemy must still close vs when strikes are allowed.
        /// Keeps NavMesh “almost there” stalls from trapping the brain outside <see cref="EnemyBrain"/> attack attempts while <see cref="EnemyAttackDriver"/> would accept the spacing.
        /// </summary>
        public float GetMaxStrikeRange(float fallback = 2.75f)
        {
            if (attacks == null || attacks.Length == 0)
                return fallback;

            float max = 0f;
            for (int i = 0; i < attacks.Length; i++)
            {
                EnemyAttackDefinition a = attacks[i];
                if (a == null)
                    continue;
                if (a.maxRange > max)
                    max = a.maxRange;
            }

            return max > 0.05f ? max : fallback;
        }

        /// <summary>
        /// NavMesh goal distance from the target when closing for melee — inside <see cref="EnemyAttackDriver"/> select range.
        /// </summary>
        public float GetMeleeClosingDistance()
        {
            float maxRange = GetMaxStrikeRange();
            float inset = maxRange - EnemyAttackDriver.AttackRangeSelectionSlack - 0.08f;
            float preferred = movement.preferredDistance > 0f ? movement.preferredDistance : inset;
            float minDist = Mathf.Max(0.35f, movement.stopDistance + 0.15f);
            return Mathf.Clamp(Mathf.Min(preferred, inset), minDist, maxRange - EnemyAttackDriver.AttackRangeSelectionSlack);
        }
    }

    [System.Serializable]
    public class EnemyPerceptionSettings
    {
        [Min(0.5f)] public float aggroRange = 12f;
        [Min(0.5f)] public float loseTargetRange = 18f;
        [Min(0f)] public float eyeHeight = 1.6f;
        public bool requiresLineOfSight = true;
        public LayerMask lineOfSightBlockers = ~0;
    }

    [System.Serializable]
    public class EnemyMovementSettings
    {
        [Min(0.1f)] public float moveSpeed = 3.5f;
        [Min(1f)] public float angularSpeed = 540f;
        [Min(0f)] public float acceleration = 20f;
        [Min(0f)] public float stopDistance = 1.6f;
        [Min(0f)] public float preferredDistance = 1.8f;
        [Min(0f)] public float distanceTolerance = 0.35f;
        [Min(0f)] public float strafeDistance = 1.8f;
        [Min(0.25f)] public float strafeRepathInterval = 1.25f;
        [Min(0f)] public float directMoveFallbackSpeed = 3f;

        [Header("Animator locomotion (Polygon / Synty)")]
        [Tooltip("Planar speed (m/s) thresholds for CurrentGait — should match player tuning when using AC_Polygon_Masculine_Geis.")]
        [Min(0.1f)] public float animatorWalkSpeedReference = Geis.Locomotion.GeisLocomotionTuningDefaults.WalkSpeed;
        [Min(0.1f)] public float animatorRunSpeedReference = Geis.Locomotion.GeisLocomotionTuningDefaults.RunSpeed;
        [Min(0.1f)] public float animatorSprintSpeedReference = Geis.Locomotion.GeisLocomotionTuningDefaults.SprintSpeed;

        [Header("Approach locomotion (animation / NavMesh speed)")]
        [Tooltip("When horizontal distance to the target is at least this value, use fast gait and run speed multiplier.")]
        [Min(0.25f)] public float approachRunDistanceThreshold = 4f;
        [Range(0.25f, 1f)]
        [Tooltip("NavMesh speed multiplier when closing inside the run threshold (jog / slower close).")]
        public float approachJogSpeedMultiplier = 0.75f;
        [Range(0.9f, 2f)]
        [Tooltip("NavMesh speed multiplier when closing beyond the run threshold.")]
        public float approachRunSpeedMultiplier = 1.45f;
        [Tooltip("Written to Animator CurrentGait when closing slowly (Polygon: 1 = Walk).")]
        public int approachSlowGait = 1;
        [Tooltip("Written to Animator CurrentGait when closing from far away (Polygon: 2 = Run, 3 = Sprint).")]
        public int approachFastGait = 2;

        [Header("Strafe locomotion")]
        [Range(0.25f, 1f)] public float strafeLocomotionSpeedMultiplier = 0.68f;
        [Tooltip("Animator CurrentGait while strafing at combat distance.")]
        public int strafeLocomotionGait = 1;
    }

    [System.Serializable]
    public class EnemyReactionSettings
    {
        [Min(0f)] public float staggerDurationOnHit = 0.2f;
        [Min(0f)] public float deathDisableDelay = 0.6f;
    }

    /// <summary>
    /// Legacy label on attack slots. When <see cref="EnemyAiDefinition.weaponDefinition"/> has <see cref="GeisWeaponDefinition.comboData"/>,
    /// <see cref="EnemyAttackDriver"/> resolves the action, scheduled multi-hit times, and combo advancement from that graph regardless of this value.
    /// </summary>
    public enum EnemyAttackActionSource
    {
        [Tooltip("Use serialized CombatAction when there is no weapon combo data, or when the combo graph does not define the current combo index.")]
        ExplicitCombatAction = 0,

        [Tooltip("Same runtime path as Explicit when a weapon combo asset is present; kept for clarity in authored data.")]
        WeaponComboResolved = 1
    }

    [System.Serializable]
    public class EnemyAttackDefinition
    {
        public string attackId = "light_swing";

        [Tooltip("Fallback CombatAction when there is no weapon combo data, or when the combo graph returns no action for the current combo index.")]
        public CombatAction action;

        [Tooltip("Legacy field; combo on the weapon drives resolution and hit timing when present.")]
        public EnemyAttackActionSource actionSource = EnemyAttackActionSource.ExplicitCombatAction;

        [Tooltip("Combo transition input after this attack when the weapon has GeisComboData (GeisComboData.TryGetNextState).")]
        public GeisComboInputType comboAdvanceInput = GeisComboInputType.Light;
        [Min(0f)] public float minRange = 0f;
        [Min(0.1f)] public float maxRange = 2.25f;
        [Tooltip("Extra wait before Attack trigger. Set to 0 when wind-up is inside the combo clip (use GeisComboData multiHitNormalizedTimes for hit frame).")]
        [Min(0f)] public float telegraphDuration = 0f;
        [Tooltip("Max telegraph wait while still moving into range (planar m/s above driver threshold). Ignored when telegraphDuration is 0.")]
        [Min(0f)] public float telegraphCapWhileMoving = 0.1f;
        [Min(0f)] public float recoveryDuration = 0.8f;
        [Min(0f)] public float cooldownSeconds = 1.25f;
        [Range(0f, 180f)] public float facingToleranceDegrees = 35f;
        public bool requiresLineOfSight = true;
        [Min(1)] public int selectionWeight = 1;
        [Tooltip("Optional separate animator trigger before Attack. Leave empty when telegraphDuration is 0 and wind-up is in the attack clip.")]
        public string telegraphTrigger = "";
        public string attackTriggerOverride;
        [Min(0f)] public float executionTimeout = 0.75f;
    }
}
