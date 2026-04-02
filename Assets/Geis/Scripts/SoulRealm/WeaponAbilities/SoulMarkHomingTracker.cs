using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Stores a soul-marked aim point for the next N bow shots (set from <see cref="SoulMarkingSoulWeaponAbility"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoulMarkHomingTracker : MonoBehaviour
    {
        private Transform _markedTransform;
        private int _homingShotsRemaining;
        private bool _homingUnlimited;

        public Transform MarkedTransform => _markedTransform;
        /// <summary>Shots left with homing, or <see cref="int.MaxValue"/> when unlimited (see <see cref="RegisterSoulMark"/>).</summary>
        public int HomingShotsRemaining => _homingUnlimited ? int.MaxValue : _homingShotsRemaining;

        /// <param name="homingShots">If 0 or negative, homing applies to every bow shot until the mark is cleared or a new mark is registered.</param>
        public void RegisterSoulMark(Transform markTransform, int homingShots)
        {
            _markedTransform = markTransform;
            if (homingShots <= 0)
            {
                _homingUnlimited = true;
                _homingShotsRemaining = 0;
            }
            else
            {
                _homingUnlimited = false;
                _homingShotsRemaining = homingShots;
            }
        }

        public void ClearSoulMarkHoming()
        {
            _markedTransform = null;
            _homingShotsRemaining = 0;
            _homingUnlimited = false;
        }

        /// <summary>
        /// If a mark is active, returns true once per shot and decrements the counter when not unlimited.
        /// </summary>
        public bool TryConsumeHomingShot(out Transform target)
        {
            target = null;
            if (_markedTransform == null)
                return false;
            if (!_homingUnlimited && _homingShotsRemaining <= 0)
                return false;
            if (!_markedTransform.gameObject.activeInHierarchy)
            {
                ClearSoulMarkHoming();
                return false;
            }

            target = _markedTransform;
            if (!_homingUnlimited)
            {
                _homingShotsRemaining--;
                if (_homingShotsRemaining <= 0)
                    _markedTransform = null;
            }
            return true;
        }
    }
}
