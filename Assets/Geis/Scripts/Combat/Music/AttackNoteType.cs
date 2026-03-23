// Geis of Anam - Combat Music System
// Attack type for note mapping (Light, Heavy, Charged, Finisher).

namespace Geis.Combat.Music
{
    /// <summary>
    /// Attack type for mapping to musical notes.
    /// Light → short note, Heavy → emphasized, Charged → higher tension, Finisher → resolving root.
    /// </summary>
    public enum AttackNoteType
    {
        Light = 0,
        Heavy = 1,
        Charged = 2,
        Finisher = 3
    }
}
