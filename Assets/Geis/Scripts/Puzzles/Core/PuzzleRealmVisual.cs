using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Realm-only presentation: tint, hide/show, or noise dissolve when entering/leaving Soul Realm.
    /// No triggers, outputs, or gameplay — use for props, set dressing, or barriers that should only exist in one realm.
    /// All options are on <see cref="PuzzleElementBase"/> (realm mode, dissolve, duration, overrides).
    /// </summary>
    [AddComponentMenu("Geis/Puzzles/Puzzle Realm Visual")]
    public sealed class PuzzleRealmVisual : PuzzleElementBase
    {
    }
}
