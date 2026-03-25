namespace Geis.Puzzles
{
    /// <summary>
    /// Defines which realm a puzzle element is accessible in.
    /// </summary>
    public enum PuzzleRealmMode
    {
        /// <summary>Only interactable/active while the soul realm is active.</summary>
        SoulOnly,

        /// <summary>Only interactable/active in the regular physical world.</summary>
        PhysicalOnly,

        /// <summary>Accessible in both realms.</summary>
        BothRealms,
    }
}
