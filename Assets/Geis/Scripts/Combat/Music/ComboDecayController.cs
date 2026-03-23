// Geis of Anam - Combat Music System
// Smoothly fades music when combo breaks. 2-3s fade, melody last so last note rings out.

using UnityEngine;

namespace Geis.Combat.Music
{
    /// <summary>
    /// When combo window expires, fades layers over 2-3 seconds. Harmony/percussion fade first,
    /// melody last so the last note sustains.
    /// </summary>
    public class ComboDecayController
    {
        private const float DecayStart = 2f;
        private const float DecayDuration = 2.5f;

        private readonly ComboTracker _tracker;
        private readonly MusicLayerManager _layerManager;
        private bool _decayActive;
        private float _decayStartTime;

        public ComboDecayController(ComboTracker tracker, MusicLayerManager layerManager)
        {
            _tracker = tracker;
            _layerManager = layerManager;
        }

        public void Update()
        {
            if (_tracker == null || _layerManager == null) return;
            if (_tracker.ComboLength == 0) return;

            if (_tracker.IsInComboWindow)
            {
                _decayActive = false;
                return;
            }

            float elapsed = Time.time - _tracker.LastAttackTime;
            if (elapsed < DecayStart) return;

            if (!_decayActive)
            {
                _decayActive = true;
                _decayStartTime = Time.time;
            }

            float t = (Time.time - _decayStartTime) / DecayDuration;
            if (t >= 1f)
            {
                _tracker.Clear();
                _layerManager.SetLayerVolumes(0f, 0f, 0f);
                return;
            }

            float harmonyPerc = 1f - t;
            float melody = t < 0.5f ? 1f : (1f - (t - 0.5f) * 2f);
            _layerManager.SetLayerVolumes(melody, harmonyPerc, harmonyPerc);
        }
    }
}
