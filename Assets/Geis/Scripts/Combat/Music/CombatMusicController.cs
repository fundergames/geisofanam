// Geis of Anam - Combat Music System
// Main coordinator: subscribes to attack events, drives combo tracking, note resolution, layers, and decay.

using UnityEngine;
using Geis.Combat;

namespace Geis.Combat.Music
{
    /// <summary>
    /// Coordinates combat music: combo tracking, note resolution, layering, and decay.
    /// Subscribe to attack events or call OnAttackPerformed directly.
    /// </summary>
    public class CombatMusicController : MonoBehaviour
    {
        public static CombatMusicController Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float comboWindow = ComboTracker.DefaultComboWindow;
        [SerializeField] private AttackNoteMapping attackNoteMapping;
        [SerializeField] private WeaponInstrumentConfig defaultInstrumentConfig;

        [Header("Audio")]
        [Tooltip("Assign UnityCombatMusicAudio or any ICombatMusicAudio component.")]
        [SerializeField] private MonoBehaviour audioProvider;

        private ICombatMusicAudio _audio;

        [Header("Weapon Instrument Override")]
        [Tooltip("Optional: map weapon index to WeaponInstrumentConfig. If null, uses default.")]
        [SerializeField] private WeaponInstrumentConfig[] weaponInstrumentOverrides;

        private ComboTracker _comboTracker;
        private MusicLayerManager _layerManager;
        private ComboDecayController _decayController;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _comboTracker = new ComboTracker(comboWindow);

            _audio = null;
            if (audioProvider is ICombatMusicAudio iaudio)
                _audio = iaudio;
            if (_audio == null)
                _audio = GetComponent<ICombatMusicAudio>();
            if (_audio == null)
                _audio = GetComponentInChildren<ICombatMusicAudio>();

            if (_audio != null && defaultInstrumentConfig != null)
                _audio.SetInstrument(defaultInstrumentConfig);

            _layerManager = new MusicLayerManager(_audio);
            _decayController = new ComboDecayController(_comboTracker, _layerManager);
        }

        private void Update()
        {
            _decayController?.Update();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// True when combat music is playing (active combo or decay phase). Use for ducking ambient.
        /// </summary>
        public bool IsCombatMusicActive => _comboTracker != null && _comboTracker.ComboLength > 0;

        /// <summary>
        /// Call when a melee attack is performed. Geis path: use GeisComboInputType and combo state.
        /// </summary>
        public void OnAttackPerformed(GeisComboInputType inputType, int comboState, int weaponIndex)
        {
            var noteType = inputType == GeisComboInputType.Heavy ? AttackNoteType.Heavy : AttackNoteType.Light;
            OnAttackPerformed(noteType, comboState, weaponIndex);
        }

        /// <summary>
        /// Call when a melee attack is performed. RogueDeal path: use AttackNoteType directly.
        /// </summary>
        public void OnAttackPerformed(AttackNoteType noteType, int comboState, int weaponIndex)
        {
            if (_audio == null) return;

            _comboTracker.OnAttack(noteType, weaponIndex);
            int comboLength = _comboTracker.ComboLength;

            var config = GetInstrumentConfig(weaponIndex);
            if (config != null)
                _audio.SetInstrument(config);

            float velocity = attackNoteMapping != null
                ? attackNoteMapping.GetVelocity(noteType)
                : NoteResolver.GetVelocity(noteType);
            int scaleIndex = NoteResolver.GetScaleIndex(noteType, comboState);

            _layerManager.OnAttack(comboLength, scaleIndex, velocity);

            _audio.PlayNote(scaleIndex, velocity, 0, 0);
        }

        private WeaponInstrumentConfig GetInstrumentConfig(int weaponIndex)
        {
            if (weaponInstrumentOverrides != null && weaponIndex >= 0 && weaponIndex < weaponInstrumentOverrides.Length)
            {
                var overrides = weaponInstrumentOverrides[weaponIndex];
                if (overrides != null) return overrides;
            }
            return defaultInstrumentConfig;
        }
    }
}
