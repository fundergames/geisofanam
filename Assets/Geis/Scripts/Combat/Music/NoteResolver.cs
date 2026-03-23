// Geis of Anam - Combat Music System
// Resolves attack type + combo position to pentatonic scale index and velocity.

namespace Geis.Combat.Music
{
    /// <summary>
    /// Resolves attack type and combo index to pentatonic scale degree (0-4) and velocity.
    /// Uses D pentatonic: D(0), F(1), G(2), A(3), C(4).
    /// </summary>
    public static class NoteResolver
    {
        private const int ScaleSize = 5;

        /// <summary>
        /// Get pentatonic scale index (0-4) for the given attack type and combo position.
        /// </summary>
        public static int GetScaleIndex(AttackNoteType type, int comboIndex)
        {
            return type switch
            {
                AttackNoteType.Light => (comboIndex + 0) % ScaleSize,
                AttackNoteType.Heavy => (comboIndex + 1) % ScaleSize,
                AttackNoteType.Charged => (comboIndex + 2) % ScaleSize,
                AttackNoteType.Finisher => 0, // Root/resolution
                _ => comboIndex % ScaleSize
            };
        }

        /// <summary>
        /// Get velocity (0-1) for the attack type. Heavy/Charged/Finisher are more emphasized.
        /// </summary>
        public static float GetVelocity(AttackNoteType type)
        {
            return type switch
            {
                AttackNoteType.Light => 0.7f,
                AttackNoteType.Heavy => 0.9f,
                AttackNoteType.Charged => 0.85f,
                AttackNoteType.Finisher => 1f,
                _ => 0.75f
            };
        }

        /// <summary>
        /// Clamp scale index to valid range.
        /// </summary>
        public static int ClampScaleIndex(int index)
        {
            return (index % ScaleSize + ScaleSize) % ScaleSize;
        }
    }
}
