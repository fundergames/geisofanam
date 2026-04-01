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

        public Transform MarkedTransform => _markedTransform;
        public int HomingShotsRemaining => _homingShotsRemaining;

        public void RegisterSoulMark(Transform markTransform, int homingShots)
        {
            _markedTransform = markTransform;
            _homingShotsRemaining = Mathf.Max(0, homingShots);
        }

        public void ClearSoulMarkHoming()
        {
            _markedTransform = null;
            _homingShotsRemaining = 0;
        }

        /// <summary>
        /// If a mark is active, returns true once per shot and decrements the counter.
        /// </summary>
        public bool TryConsumeHomingShot(out Transform target)
        {
            target = null;
            if (_markedTransform == null || _homingShotsRemaining <= 0)
                return false;
            if (!_markedTransform.gameObject.activeInHierarchy)
            {
                ClearSoulMarkHoming();
                return false;
            }

            target = _markedTransform;
            _homingShotsRemaining--;
            if (_homingShotsRemaining <= 0)
                _markedTransform = null;
            return true;
        }
    }
}
