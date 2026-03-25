using Geis.SoulRealm;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Base for all puzzle triggers and outputs. Handles realm-gating so individual
    /// subclasses don't need to repeat the realm check.
    /// </summary>
    public abstract class PuzzleElementBase : MonoBehaviour
    {
        [Header("Realm")]
        [Tooltip("Which realm this element is accessible in.")]
        [SerializeField] private PuzzleRealmMode realmMode = PuzzleRealmMode.SoulOnly;

        public PuzzleRealmMode RealmMode => realmMode;

        /// <summary>
        /// Returns true when this element should be active based on the current realm state.
        /// </summary>
        protected bool IsAccessibleInCurrentRealm()
        {
            bool soulActive = SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
            return realmMode switch
            {
                PuzzleRealmMode.SoulOnly     => soulActive,
                PuzzleRealmMode.PhysicalOnly => !soulActive,
                PuzzleRealmMode.BothRealms   => true,
                _                            => false,
            };
        }
    }
}
