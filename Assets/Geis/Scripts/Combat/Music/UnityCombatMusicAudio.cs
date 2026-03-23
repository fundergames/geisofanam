// Geis of Anam - Combat Music System
// Unity AudioSource implementation using Musical_Instruments_And_Notes clips.

using UnityEngine;

namespace Geis.Combat.Music
{
    /// <summary>
    /// Implements ICombatMusicAudio with Unity AudioSource. Uses WeaponInstrumentConfig.pentatonicClips.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class UnityCombatMusicAudio : MonoBehaviour, ICombatMusicAudio
    {
        [Header("Configuration")]
        [SerializeField] private WeaponInstrumentConfig defaultInstrumentConfig;

        [Tooltip("Master volume for attack notes (0 = silent, 1 = full).")]
        [Range(0f, 2f)]
        [SerializeField] private float masterVolume = 1f;

        /// <summary>Master volume for all combat-music notes. Adjust at runtime if needed.</summary>
        public float MasterVolume { get => masterVolume; set => masterVolume = Mathf.Clamp(value, 0f, 2f); }

        [Header("Audio Sources (Optional - one per layer)")]
        [Tooltip("Melody layer AudioSource. If null, uses main.")]
        [SerializeField] private AudioSource melodySource;
        [Tooltip("Harmony layer AudioSource. If null, uses main.")]
        [SerializeField] private AudioSource harmonySource;
        [Tooltip("Percussion layer AudioSource. If null, uses main.")]
        [SerializeField] private AudioSource percussionSource;

        private AudioSource _mainSource;
        private WeaponInstrumentConfig _currentConfig;

        private void Awake()
        {
            _mainSource = GetComponent<AudioSource>();
            if (_mainSource == null) _mainSource = gameObject.AddComponent<AudioSource>();
            _mainSource.playOnAwake = false;
            _mainSource.loop = false;

            if (melodySource == null)
            {
                melodySource = CreateLayerSource("Melody");
            }
            if (harmonySource == null)
            {
                harmonySource = CreateLayerSource("Harmony");
            }
            if (percussionSource == null)
            {
                percussionSource = CreateLayerSource("Percussion");
            }

            if (defaultInstrumentConfig != null)
                SetInstrument(defaultInstrumentConfig);
        }

        private AudioSource CreateLayerSource(string layerName)
        {
            var go = new GameObject($"CombatMusic_{layerName}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            return src;
        }

        public void PlayNote(int scaleIndex, float velocity, float durationMs, int layerIndex)
        {
            var config = _currentConfig ?? defaultInstrumentConfig;
            if (config == null) return;

            AudioClip clip = null;
            int idx = NoteResolver.ClampScaleIndex(scaleIndex);
            if (layerIndex == 1 && config.HasHarmonyClips)
                clip = config.GetHarmonyClip(idx);
            if (clip == null && layerIndex == 2 && config.HasPercussionClips)
                clip = config.GetPercussionClip(scaleIndex % 5);
            if (clip == null)
                clip = config.GetMelodyClip(idx);
            if (clip == null) return;

            var source = GetSourceForLayer(layerIndex);
            if (source != null)
                source.PlayOneShot(clip, velocity * masterVolume);
        }

        public void SetLayerVolume(int layer, float volume)
        {
            var source = GetSourceForLayer(layer);
            if (source != null)
                source.volume = Mathf.Clamp01(volume);
        }

        public void SetInstrument(WeaponInstrumentConfig config)
        {
            _currentConfig = config;
        }

        private AudioSource GetSourceForLayer(int layerIndex)
        {
            return layerIndex switch
            {
                0 => melodySource,
                1 => harmonySource,
                2 => percussionSource,
                _ => _mainSource
            };
        }
    }
}
