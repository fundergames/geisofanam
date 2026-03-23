// Geis of Anam - Combat Music System
// Manages layer activation based on combo length: 1-2 melody, 3-4 +harmony, 5-6 +percussion, 7+ full.

namespace Geis.Combat.Music
{
    /// <summary>
    /// Activates and fades layers based on combo length.
    /// 1-2 hits: melody only. 3-4: +harmony. 5-6: +percussion. 7+: full arrangement.
    /// </summary>
    public class MusicLayerManager
    {
        private readonly ICombatMusicAudio _audio;
        private int _lastComboLength = -1;

        private const int HarmonyThreshold = 3;
        private const int PercussionThreshold = 5;
        private const int FullThreshold = 7;

        public MusicLayerManager(ICombatMusicAudio audio)
        {
            _audio = audio;
        }

        /// <summary>
        /// Called when an attack is performed. Updates layer volumes and plays harmony/percussion if applicable.
        /// </summary>
        public void OnAttack(int comboLength, int scaleIndex, float velocity)
        {
            if (_audio == null) return;

            UpdateLayerVolumes(comboLength);

            if (comboLength >= HarmonyThreshold)
                _audio.PlayNote(scaleIndex, velocity * 0.7f, 0, 1);
            if (comboLength >= PercussionThreshold)
                _audio.PlayNote(scaleIndex % 5, 0.5f, 0, 2);

            _lastComboLength = comboLength;
        }

        /// <summary>
        /// Set layer volumes based on combo length. Called by decay controller during fade.
        /// </summary>
        public void SetLayerVolumes(float melody, float harmony, float percussion)
        {
            if (_audio == null) return;
            _audio.SetLayerVolume(0, melody);
            _audio.SetLayerVolume(1, harmony);
            _audio.SetLayerVolume(2, percussion);
        }

        private void UpdateLayerVolumes(int comboLength)
        {
            if (_audio == null) return;

            float melody = comboLength >= 1 ? 1f : 0f;
            float harmony = comboLength >= HarmonyThreshold ? 1f : 0f;
            float percussion = comboLength >= PercussionThreshold ? 1f : 0f;

            _audio.SetLayerVolume(0, melody);
            _audio.SetLayerVolume(1, harmony);
            _audio.SetLayerVolume(2, percussion);
        }
    }
}
