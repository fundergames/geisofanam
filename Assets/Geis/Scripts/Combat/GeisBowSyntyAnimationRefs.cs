// Geis of Anam — Reference for Synty AnimationBowCombat (Polygon) assets used with bow gameplay.
// PDF: Assets/Synty/AnimationBowCombat/Documentation/ANIMATION_BowCombat_UserGuide.pdf

namespace Geis.Combat
{
    /// <summary>
    /// Documents Synty <b>AnimationBowCombat</b> Polygon clip naming and layout. The animator <c>Bow_Draw</c> state
    /// uses <see cref="ClipReloadNeutral"/> by default (see <c>AC_Polygon_Masculine_Geis</c>).
    /// </summary>
    /// <remarks>
    /// <para><b>Folder layout</b> (relative to <c>Assets/Synty/AnimationBowCombat/Animations/Polygon/</c>):</para>
    /// <list type="bullet">
    /// <item><c>Neutral/Standing/Shoot/</c> — generic bow (no prop suffix): ToAiming, ToBowDown, Reload, Leaping, Dive, etc.</item>
    /// <item><c>Neutral/Standing/Shoot/Lng/</c> — longbow-specific clips</item>
    /// <item><c>Neutral/Standing/Shoot/Rcv/</c> — recurve</item>
    /// <item><c>Neutral/Standing/Shoot/Cmp/</c> — compound</item>
    /// <item><c>Neutral/Standing/Idle/</c> — bow idle, equip, aim idle, bow-down</item>
    /// </list>
    /// <para><b>Typical shoot flow</b> (matches Synty timelines under <c>Samples/Timelines/Polygon/</c>):</para>
    /// <list type="number">
    /// <item><c>Stand_Shoot_ToAiming</c> — transition into aim-ready pose</item>
    /// <item><c>Stand_Shoot_Reload</c> — nock / draw / release cycle (also used as draw hold in Geis)</item>
    /// <item><c>Stand_Shoot_ToBowDown</c> — lower bow after shot</item>
    /// </list>
    /// <para><b>Reload FBX</b> also exposes sub-clips: <c>Stand_Shoot_Only</c> (short release), <c>Reload_Only</c> (subset) — use
    /// in AnimatorOverrideController if you split draw vs release.</para>
    /// <para><b>Hold RT</b>: enable <b>Loop Time</b> on the Reload clip in the FBX Animation import settings so the draw
    /// loops while <c>BowDrawing</c> is true.</para>
    /// <para><b>LT aim</b>: <see cref="ParamBowAiming"/> is set by <c>GeisPlayerAnimationController</c> while aim is held and the bow
    /// is equipped (slot 3). The <c>Bow_Draw</c> layer runs <c>Bow_ToAiming</c> → <c>Bow_AimHold</c> (uses slice <c>ToAiming_Neut_End</c>)
    /// → <c>Bow_ToBowDown</c> when releasing LT. Enable <b>Loop Time</b> on <c>ToAiming_Neut_End</c> in the ToAiming FBX import for a steady aim pose.</para>
    /// </remarks>
    public static class GeisBowSyntyAnimationRefs
    {
        public const string DocumentationPdf =
            "Assets/Synty/AnimationBowCombat/Documentation/ANIMATION_BowCombat_UserGuide.pdf";

        public const string AnimationsRoot =
            "Assets/Synty/AnimationBowCombat/Animations/Polygon";

        /// <summary>FBX: <c>A_POLY_BOW_Stand_Shoot_Reload_Neut.fbx</c> — default draw/nock cycle (Animator Bow_Draw).</summary>
        public const string ClipReloadNeutral = "A_POLY_BOW_Stand_Shoot_Reload_Neut";

        public const string ClipToAimingNeutral = "A_POLY_BOW_Stand_Shoot_ToAiming_Neut";
        public const string ClipToBowDownNeutral = "A_POLY_BOW_Stand_Shoot_ToBowDown_Neut";
        public const string ClipShootOnlyNeutral = "A_POLY_BOW_Stand_Shoot_Only_Neut";
        public const string ClipReloadOnlyNeutral = "A_POLY_BOW_Stand_Shoot_Reload_Only_Neut";

        /// <summary>Second slice on the ToAiming FBX — hold pose while <c>BowAiming</c> is true (Animator state <c>Bow_AimHold</c>).</summary>
        public const string ClipToAimingEndNeutral = "A_POLY_BOW_Stand_Shoot_ToAiming_Neut_End";

        /// <summary>Animator bool: bow equipped + LT held — drives ToAiming / aim hold / ToBowDown on the Bow_Draw layer.</summary>
        public const string ParamBowAiming = "BowAiming";
    }
}
