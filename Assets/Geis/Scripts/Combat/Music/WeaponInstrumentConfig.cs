// Geis of Anam - Combat Music System
// ScriptableObject for per-weapon instrument configuration using Musical_Instruments_And_Notes.

using UnityEngine;

namespace Geis.Combat.Music
{
    /// <summary>
    /// Per-weapon instrument configuration. Pentatonic clips (D4, F4, G4, A4, C5) from
    /// Musical_Instruments_And_Notes. Optional harmony and percussion instruments for layered combos.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponInstrumentConfig_", menuName = "Geis/Combat/Music/Weapon Instrument Config")]
    public class WeaponInstrumentConfig : ScriptableObject
    {
        [Tooltip("Instrument folder name under Musical_Instruments_And_Notes/Wave/44_1kHz-16bit/")]
        [SerializeField] private string instrumentFolderName = "Marimba";

        [Tooltip("Pentatonic clips (D4, F4, G4, A4, C5) - assign from instrument folder or use auto-populate menu")]
        [SerializeField] private AudioClip[] pentatonicClips = new AudioClip[5];

        [Header("Layered Combo (Optional)")]
        [Tooltip("Harmony instrument clips (same pentatonic). Used when combo >= 3.")]
        [SerializeField] private AudioClip[] harmonyPentatonicClips;

        [Tooltip("Percussion clips for combo >= 5. Can use Tuned_Percussion or Bell_Glockenspiel notes.")]
        [SerializeField] private AudioClip[] percussionClips;

        public string InstrumentFolderName => instrumentFolderName;

        /// <summary>
        /// Get the melody pentatonic clip for scale index 0-4. Returns null if invalid.
        /// </summary>
        public AudioClip GetMelodyClip(int scaleIndex)
        {
            if (pentatonicClips == null || scaleIndex < 0 || scaleIndex >= pentatonicClips.Length)
                return null;
            return pentatonicClips[scaleIndex];
        }

        /// <summary>
        /// Get harmony clip for scale index. Returns null if not configured.
        /// </summary>
        public AudioClip GetHarmonyClip(int scaleIndex)
        {
            if (harmonyPentatonicClips == null || harmonyPentatonicClips.Length < 5 ||
                scaleIndex < 0 || scaleIndex >= harmonyPentatonicClips.Length)
                return null;
            return harmonyPentatonicClips[scaleIndex];
        }

        /// <summary>
        /// Get percussion clip (e.g. for rhythm layer). Index can cycle if fewer clips than scale.
        /// </summary>
        public AudioClip GetPercussionClip(int index)
        {
            if (percussionClips == null || percussionClips.Length == 0) return null;
            return percussionClips[index % percussionClips.Length];
        }

        /// <summary>
        /// True if melody clips are properly configured.
        /// </summary>
        public bool HasMelodyClips
        {
            get
            {
                if (pentatonicClips == null || pentatonicClips.Length < 5) return false;
                for (int i = 0; i < 5; i++)
                    if (pentatonicClips[i] == null) return false;
                return true;
            }
        }

        /// <summary>
        /// True if harmony layer is configured.
        /// </summary>
        public bool HasHarmonyClips =>
            harmonyPentatonicClips != null && harmonyPentatonicClips.Length >= 5;

        /// <summary>
        /// True if percussion layer is configured.
        /// </summary>
        public bool HasPercussionClips =>
            percussionClips != null && percussionClips.Length > 0;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Set pentatonic clips (used by WeaponInstrumentConfigEditor).
        /// </summary>
        public void SetPentatonicClips(AudioClip[] clips)
        {
            if (clips != null && clips.Length >= 5)
                pentatonicClips = clips;
        }
#endif
    }
}
