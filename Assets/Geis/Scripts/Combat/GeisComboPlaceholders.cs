// Geis of Anam - Holds references to 32 placeholder clips for combo blend tree.
// Used by AnimatorOverrideController at runtime to swap in GeisComboData clips.
// Create via Tools > Geis > Create Combo Placeholder Clips.

using UnityEngine;

namespace Geis.Combat
{
    /// <summary>
    /// References to 32 placeholder clips. The Attack blend tree uses these;
    /// at runtime we override them with GeisComboData clips.
    /// </summary>
    public class GeisComboPlaceholders : ScriptableObject
    {
        [SerializeField]
        private AnimationClip[] placeholders = new AnimationClip[32];

        public AnimationClip GetPlaceholder(int index)
        {
            if (placeholders == null || index < 0 || index >= placeholders.Length)
                return null;
            return placeholders[index];
        }

        public int Count => placeholders != null ? placeholders.Length : 0;
    }
}
