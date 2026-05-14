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
using UnityEngine;

namespace Geis.Enemies
{
    public class EnemyEncounterController : MonoBehaviour
    {
        [SerializeField] private EnemyCombatant[] managedEnemies;
        [SerializeField] private bool autoStartOnAwake = true;
        [SerializeField] private bool autoLoopOnClear = true;
        [SerializeField] private float resetDelaySeconds = 3f;

        private Coroutine _resetRoutine;

        private void Awake()
        {
            if (managedEnemies == null || managedEnemies.Length == 0)
                managedEnemies = GetComponentsInChildren<EnemyCombatant>(true);
        }

        private void Start()
        {
            if (autoStartOnAwake)
                StartEncounter();
        }

        private void Update()
        {
            if (!autoLoopOnClear || managedEnemies == null || managedEnemies.Length == 0 || _resetRoutine != null)
                return;

            for (int i = 0; i < managedEnemies.Length; i++)
            {
                EnemyCombatant enemy = managedEnemies[i];
                if (enemy != null && !enemy.IsDefeated)
                    return;
            }

            _resetRoutine = StartCoroutine(ResetAfterDelay());
        }

        public void StartEncounter()
        {
            ResetEncounter();
        }

        public void ResetEncounter()
        {
            if (_resetRoutine != null)
            {
                StopCoroutine(_resetRoutine);
                _resetRoutine = null;
            }

            if (managedEnemies == null)
                return;

            for (int i = 0; i < managedEnemies.Length; i++)
            {
                EnemyCombatant enemy = managedEnemies[i];
                if (enemy == null)
                    continue;

                enemy.ResetCombatant();
            }
        }

        private IEnumerator ResetAfterDelay()
        {
            yield return new WaitForSeconds(resetDelaySeconds);
            ResetEncounter();
        }
    }
}
