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

using RogueDeal.Combat;
using UnityEngine;

namespace Geis.Enemies
{
    public enum EnemyCombatRole
    {
        Frontliner = 0,
        Skirmisher = 1,
        Support = 2,
        Anchor = 3
    }

    /// <summary>
    /// Passive coordination data for future squad behaviors.
    /// Phase 1 uses this as metadata only; enemies remain fully functional without a coordinator.
    /// </summary>
    public class EnemyCoordinationContext : MonoBehaviour
    {
        [SerializeField] private string squadId;
        [SerializeField] private EnemyCombatRole combatRole = EnemyCombatRole.Frontliner;
        [SerializeField] private float reservationCooldownSeconds = 1f;

        private CombatEntity _engagedTarget;
        private float _lastReservationTime = float.NegativeInfinity;

        public string SquadId
        {
            get => squadId;
            set => squadId = value;
        }

        public EnemyCombatRole CombatRole
        {
            get => combatRole;
            set => combatRole = value;
        }

        public CombatEntity EngagedTarget => _engagedTarget;
        public bool HasRecentReservation => Time.time < _lastReservationTime + reservationCooldownSeconds;

        public void ApplyDefinition(EnemyAiDefinition definition)
        {
            if (definition == null)
                return;

            squadId = definition.defaultSquadId;
            combatRole = definition.defaultCombatRole;
        }

        public void MarkEngagedTarget(CombatEntity target)
        {
            _engagedTarget = target;
        }

        public bool TryReserveAttackWindow(CombatEntity target)
        {
            if (target == null)
                return false;

            if (HasRecentReservation && _engagedTarget == target)
                return false;

            _engagedTarget = target;
            _lastReservationTime = Time.time;
            return true;
        }

        public void ClearReservation()
        {
            _engagedTarget = null;
            _lastReservationTime = float.NegativeInfinity;
        }

        public void ResetContext()
        {
            _engagedTarget = null;
            _lastReservationTime = float.NegativeInfinity;
        }
    }
}
