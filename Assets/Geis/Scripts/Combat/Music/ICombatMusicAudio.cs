// Geis of Anam - Combat Music System
// Audio abstraction for combat music (Unity, FMOD, Wwise).

namespace Geis.Combat.Music
{
    /// <summary>
    /// Audio output abstraction for combat music. Implement with Unity AudioSource, FMOD, or Wwise.
    /// </summary>
    public interface ICombatMusicAudio
    {
        /// <summary>
        /// Play a note at the given pentatonic scale index (0-4), with velocity and optional duration.
        /// </summary>
        void PlayNote(int scaleIndex, float velocity, float durationMs, int layerIndex);

        /// <summary>
        /// Set volume for a layer (0=melody, 1=harmony, 2=percussion, etc.).
        /// </summary>
        void SetLayerVolume(int layer, float volume);

        /// <summary>
        /// Switch to the given weapon instrument configuration.
        /// </summary>
        void SetInstrument(WeaponInstrumentConfig config);
    }
}
