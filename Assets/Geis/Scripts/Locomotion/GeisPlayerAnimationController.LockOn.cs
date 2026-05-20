/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Geis.Combat;
using Geis.InputSystem;
using Geis.InteractInput;
using Geis.Attributes;
using Geis.Animation;
using Geis.SoulRealm;
using RogueDeal.Combat;
using RogueDeal.Combat.Targeting;

namespace Geis.Locomotion
{
    public partial class GeisPlayerAnimationController
    {
        #region Aim and Lock-on

        /// <summary>
        ///     Activates the aim action of the player.
        /// </summary>
        private void ActivateAim()
        {
            _isAiming = true;

            if (IsBowEquipped)
                DeactivateSprint();

            _isStrafing = !_isSprinting;
        }

        /// <summary>
        ///     Deactivates the aim action of the player.
        /// </summary>
        private void DeactivateAim()
        {
            _isAiming = false;
            _isStrafing = !_isSprinting && (_alwaysStrafe || _isLockedOn);
        }

        /// <summary>
        ///     Adds an object to the list of target candidates.
        /// </summary>
        /// <param name="newTarget">The object to add.</param>
        public void AddTargetCandidate(GameObject newTarget)
        {
            if (newTarget != null)
            {
                _currentTargetCandidates.Add(newTarget);
            }
        }

        /// <summary>
        ///     Removes an object to the list of target candidates if present.
        /// </summary>
        /// <param name="targetToRemove">The object to remove if present.</param>
        public void RemoveTarget(GameObject targetToRemove)
        {
            if (_currentTargetCandidates.Contains(targetToRemove))
            {
                _currentTargetCandidates.Remove(targetToRemove);
            }
        }

        /// <summary>
        ///     Toggle the lock-on state.
        /// </summary>
        private void ToggleLockOn()
        {
            EnableLockOn(!_isLockedOn);
        }

        private void CycleLockOnLeft()
        {
            CycleLockOnTarget(-1);
        }

        private void CycleLockOnRight()
        {
            CycleLockOnTarget(1);
        }

        /// <summary>
        ///     Sets the lock-on state to the given state.
        /// </summary>
        /// <param name="enable">The state to set lock-on to.</param>
        private void EnableLockOn(bool enable)
        {
            _isLockedOn = enable;
            _isStrafing = false;

            _isStrafing = enable ? !_isSprinting : _alwaysStrafe || _isAiming;

            if (_targetLockOnPos != null)
                _cameraController.LockOn(enable, _targetLockOnPos);

            if (enable && _currentLockOnTarget != null)
            {
                _currentLockOnTarget.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(true, true);
            }
        }

        private void CycleLockOnTarget(int direction)
        {
            if (!_isLockedOn || _currentTargetCandidates.Count == 0)
                return;

            var orderedTargets = GetOrderedLockOnTargets();
            if (orderedTargets.Count == 0)
                return;

            if (_currentLockOnTarget == null)
            {
                _currentLockOnTarget = direction < 0 ? orderedTargets[orderedTargets.Count - 1] : orderedTargets[0];
            }
            else
            {
                int currentIndex = orderedTargets.IndexOf(_currentLockOnTarget);
                if (currentIndex < 0)
                {
                    _currentLockOnTarget = direction < 0 ? orderedTargets[orderedTargets.Count - 1] : orderedTargets[0];
                }
                else
                {
                    int step = direction < 0 ? -1 : 1;
                    int nextIndex = (currentIndex + step + orderedTargets.Count) % orderedTargets.Count;
                    _currentLockOnTarget = orderedTargets[nextIndex];
                }
            }

            HighlightCurrentLockOnTarget();
            UpdateLockOnAnchorPosition();
        }

        #endregion
        #region Lock-on System

        /// <summary>
        /// Uses ghost position in soul realm so lock-on candidate scoring matches the active avatar.
        /// </summary>
        private Vector3 GetLockOnDistanceEvaluationPosition()
        {
            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
                return SoulRealmManager.Instance.GetInteractionProximityWorldPosition();
            return transform.position;
        }

        /// <summary>
        /// Keeps the player lock-on aim point on the enemy while the body root does not move (soul realm).
        /// </summary>
        private void UpdateLockOnAnchorPosition()
        {
            if (_isLockedOn && _targetLockOnPos != null && _currentLockOnTarget != null)
            {
                _targetLockOnPos.position = ResolveLockOnWorldPosition(_currentLockOnTarget);
                _lockOnIndicator?.SetAnchorTarget(_targetLockOnPos);
            }
            else
            {
                _lockOnIndicator?.ClearTarget();
            }
        }

        private Vector3 ResolveLockOnWorldPosition(GameObject target)
        {
            if (target == null)
                return Vector3.zero;

            CombatEntity entity = target.GetComponent<CombatEntity>()
                ?? target.GetComponentInParent<CombatEntity>()
                ?? target.GetComponentInChildren<CombatEntity>();

            if (entity != null)
            {
                if (entity.hitPoint != null)
                    return entity.hitPoint.position;

                Renderer[] entityRenderers = entity.GetComponentsInChildren<Renderer>();
                if (TryGetBoundsCenter(entityRenderers, out var entityCenter))
                    return entityCenter;
            }

            Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>();
            if (TryGetBoundsCenter(targetRenderers, out var targetCenter))
                return targetCenter;

            return target.transform.position;
        }

        private static bool TryGetBoundsCenter(Renderer[] renderers, out Vector3 center)
        {
            center = Vector3.zero;
            if (renderers == null || renderers.Length == 0)
                return false;

            Bounds combinedBounds = default;
            bool foundBounds = false;

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!foundBounds)
                {
                    combinedBounds = renderer.bounds;
                    foundBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!foundBounds)
                return false;

            center = combinedBounds.center;
            return true;
        }

        private List<GameObject> GetOrderedLockOnTargets()
        {
            var orderedTargets = new List<(GameObject target, float angle, float distance)>();

            foreach (GameObject target in _currentTargetCandidates)
            {
                if (target == null || !target.activeInHierarchy)
                    continue;

                Vector3 targetPosition = ResolveLockOnWorldPosition(target);
                float angle = GetLockOnHorizontalAngle(targetPosition);
                float distance = Vector3.SqrMagnitude(targetPosition - GetLockOnDistanceEvaluationPosition());
                orderedTargets.Add((target, angle, distance));
            }

            orderedTargets.Sort((a, b) =>
            {
                int angleCompare = a.angle.CompareTo(b.angle);
                return angleCompare != 0 ? angleCompare : a.distance.CompareTo(b.distance);
            });

            var result = new List<GameObject>(orderedTargets.Count);
            foreach (var targetInfo in orderedTargets)
                result.Add(targetInfo.target);

            return result;
        }

        private float GetLockOnHorizontalAngle(Vector3 targetPosition)
        {
            Vector3 fromCamera = targetPosition - _cameraController.GetCameraPosition();
            fromCamera.y = 0f;
            if (fromCamera.sqrMagnitude <= 0.0001f)
                return 0f;

            Vector3 cameraForward = _cameraController.GetCameraForward();
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude <= 0.0001f)
                cameraForward = transform.forward;
            cameraForward.Normalize();

            Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
            fromCamera.Normalize();

            float horizontal = Vector3.Dot(fromCamera, cameraRight);
            float vertical = Vector3.Dot(fromCamera, cameraForward);
            return Mathf.Atan2(horizontal, vertical);
        }

        private void HighlightCurrentLockOnTarget()
        {
            foreach (GameObject target in _currentTargetCandidates)
            {
                if (target == null)
                    continue;

                target.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(target == _currentLockOnTarget, target == _currentLockOnTarget);
            }
        }

        /// <summary>
        ///     Updates and sets the best target for lock on from the list of available targets.
        /// </summary>
        private void UpdateBestTarget()
        {
            GameObject newBestTarget;

            if (_currentTargetCandidates.Count == 0)
            {
                newBestTarget = null;
            }
            else if (_currentTargetCandidates.Count == 1)
            {
                newBestTarget = _currentTargetCandidates[0];
            }
            else
            {
                newBestTarget = null;
                float bestTargetScore = 0f;

                foreach (GameObject target in _currentTargetCandidates)
                {
                    target.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(false, false);

                    Vector3 targetPosition = ResolveLockOnWorldPosition(target);
                    float distance = Vector3.Distance(GetLockOnDistanceEvaluationPosition(), targetPosition);
                    float distanceScore = 1 / distance * 100;

                    Vector3 targetDirection = targetPosition - _cameraController.GetCameraPosition();
                    float angleInView = Vector3.Dot(targetDirection.normalized, _cameraController.GetCameraForward());
                    float angleScore = angleInView * 40;

                    float totalScore = distanceScore + angleScore;

                    if (totalScore > bestTargetScore)
                    {
                        bestTargetScore = totalScore;
                        newBestTarget = target;
                    }
                }
            }

            if (!_isLockedOn)
            {
                _currentLockOnTarget = newBestTarget;

                if (_currentLockOnTarget != null)
                {
                    _currentLockOnTarget.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(true, false);
                }
            }
            else
            {
                if (_currentTargetCandidates.Contains(_currentLockOnTarget))
                {
                    _currentLockOnTarget.GetComponent<Geis.Combat.GeisObjectLockOn>()?.Highlight(true, true);
                }
                else
                {
                    _currentLockOnTarget = newBestTarget;
                    EnableLockOn(false);
                }
            }
        }

        #endregion
    }
}
