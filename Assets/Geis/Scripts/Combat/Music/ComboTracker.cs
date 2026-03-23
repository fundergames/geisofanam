// Geis of Anam - Combat Music System
// Tracks recent attacks, combo length, and timing window for music layering.

using System.Collections.Generic;

namespace Geis.Combat.Music
{
    /// <summary>
    /// Tracks recent attacks for combo detection. Uses a timing window to reset when combo breaks.
    /// </summary>
    public class ComboTracker
    {
        public const float DefaultComboWindow = 2.5f;
        private const int MaxAttackHistory = 10;

        private readonly Queue<AttackRecord> _recentAttacks = new();
        private float _lastAttackTime = -999f;
        private readonly float _comboWindow;

        public ComboTracker(float comboWindow = DefaultComboWindow)
        {
            _comboWindow = comboWindow > 0 ? comboWindow : DefaultComboWindow;
        }

        /// <summary>
        /// Number of attacks in the current combo.
        /// </summary>
        public int ComboLength => _recentAttacks.Count;

        /// <summary>
        /// Time of the most recent attack.
        /// </summary>
        public float LastAttackTime => _lastAttackTime;

        /// <summary>
        /// True if we're still within the combo window (no reset yet).
        /// </summary>
        public bool IsInComboWindow => UnityEngine.Time.time - _lastAttackTime <= _comboWindow;

        /// <summary>
        /// Record an attack and update combo state.
        /// </summary>
        public void OnAttack(AttackNoteType type, int weaponIndex)
        {
            float now = UnityEngine.Time.time;
            if (now - _lastAttackTime > _comboWindow)
                _recentAttacks.Clear();

            _recentAttacks.Enqueue(new AttackRecord(type, now, weaponIndex));
            while (_recentAttacks.Count > MaxAttackHistory)
                _recentAttacks.Dequeue();
            _lastAttackTime = now;
        }

        /// <summary>
        /// Clear combo (e.g. when decay finishes).
        /// </summary>
        public void Clear()
        {
            _recentAttacks.Clear();
        }

        /// <summary>
        /// Get the last attack record for decay/layering logic.
        /// </summary>
        public AttackRecord? GetLastAttack()
        {
            if (_recentAttacks.Count == 0) return null;
            var arr = _recentAttacks.ToArray();
            return arr[arr.Length - 1];
        }

        public struct AttackRecord
        {
            public AttackNoteType Type;
            public float Time;
            public int WeaponIndex;

            public AttackRecord(AttackNoteType type, float time, int weaponIndex)
            {
                Type = type;
                Time = time;
                WeaponIndex = weaponIndex;
            }
        }
    }
}
