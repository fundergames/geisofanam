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
        public bool DashPressed;
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
