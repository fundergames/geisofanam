// Geis of Anam - Combat Music System
// Plays ambient/background music. Ducks when combat music is active.

using UnityEngine;

namespace Geis.Combat.Music
{
    /// <summary>
    /// Manages ambient background music. Optionally ducks volume when combat music is active.
    /// Can use a direct clip or WorldDefinition.backgroundMusic for level-specific ambient.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbientMusicManager : MonoBehaviour
    {
        [Header("Music")]
        [Tooltip("Ambient clip to play. Used if no World Definition is set.")]
        [SerializeField] private AudioClip ambientClip;

        [Tooltip("Optional: use this world's backgroundMusic instead of Ambient Clip.")]
        [SerializeField] private RogueDeal.Levels.WorldDefinition worldDefinition;

        [Header("Ducking (Combat Integration)")]
        [Tooltip("Duck ambient volume when combat music is active.")]
        [SerializeField] private bool duckDuringCombat = true;

        [Tooltip("Ambient volume when combat music is playing (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float duckedVolume = 0.25f;

        [Tooltip("Smooth transition speed for volume changes.")]
        [SerializeField] private float volumeLerpSpeed = 3f;

        private AudioSource _audioSource;
        private float _targetVolume = 1f;
        private float _fullVolume = 1f;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = true;

            AudioClip clip = ambientClip;
            if (worldDefinition != null && worldDefinition.backgroundMusic != null)
                clip = worldDefinition.backgroundMusic;

            if (clip != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
            }
            else if (ambientClip == null && (worldDefinition == null || worldDefinition.backgroundMusic == null))
            {
                Debug.LogWarning("[AmbientMusicManager] No ambient clip assigned. Assign Ambient Clip or World Definition with backgroundMusic.");
            }

            _fullVolume = _audioSource.volume;
            _targetVolume = _fullVolume;
        }

        private void Update()
        {
            if (_audioSource == null) return;

            if (duckDuringCombat && CombatMusicController.Instance != null)
            {
                _targetVolume = CombatMusicController.Instance.IsCombatMusicActive ? duckedVolume : _fullVolume;
            }
            else
            {
                _targetVolume = _fullVolume;
            }

            _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, _targetVolume, volumeLerpSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Set the ambient clip at runtime (e.g. when loading a new level).
        /// </summary>
        public void SetAmbientClip(AudioClip clip)
        {
            if (clip == null) return;
            _audioSource.clip = clip;
            _audioSource.Play();
        }

        /// <summary>
        /// Set ambient from WorldDefinition (uses backgroundMusic).
        /// </summary>
        public void SetWorld(RogueDeal.Levels.WorldDefinition world)
        {
            worldDefinition = world;
            if (world != null && world.backgroundMusic != null && _audioSource != null)
            {
                _audioSource.clip = world.backgroundMusic;
                _audioSource.Play();
            }
        }
    }
}
