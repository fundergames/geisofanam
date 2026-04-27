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

using UnityEngine;

namespace RogueDeal.Combat
{
    /// <summary>
    /// Abstract combat input for the current frame. Consumed by camera and movement;
    /// filled by an input provider (e.g. CombatInputReader).
    /// </summary>
    public struct CombatInputState
    {
        public Vector2 Move;
        /// <summary>Look delta this frame (x = yaw, y = pitch). Same units for mouse and gamepad.</summary>
        public Vector2 Look;
        public bool Run;
        /// <summary>Sprint (hold) - typically same as Run for Polygon-style locomotion.</summary>
        public bool SprintHeld;
        /// <summary>Crouch toggle pressed this frame.</summary>
        public bool CrouchPressed;
        /// <summary>Lock-on toggle pressed this frame (Q or right stick click).</summary>
        public bool LockOnPressed;
        /// <summary>Switch lock-on target to the left (left arrow or d-pad left).</summary>
        public bool CycleLockOnLeftPressed;
        /// <summary>Switch lock-on target to the right (right arrow or d-pad right).</summary>
        public bool CycleLockOnRightPressed;
        public bool DodgePressed;
        /// <summary>Jump (e.g. space) - used by Polygon-style controller.</summary>
        public bool JumpPressed;
        public bool AttackPressed;
        /// <summary>True when AttackPressed came from a mouse click (use for click-to-target).</summary>
        public bool HasAttackClickPosition;
        public Vector2 AttackClickScreenPosition;
    }

    /// <summary>
    /// Provides abstract combat input. Implementations read from keyboard/mouse/gamepad
    /// and handle device switching; consumers only see CombatInputState.
    /// </summary>
    public interface ICombatInputProvider
    {
        CombatInputState GetState();
    }
}
