/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using System.Collections.Generic;
using UnityEngine;

namespace RogueDeal.Combat.Presentation
{
    /// <summary>
    /// Short hit-stop windows (real-time). Stack-safe; cancel by attack generation token.
    /// </summary>
    public class CombatHitStopService : MonoBehaviour
    {
        private const float BaseFixedDeltaTime = 0.02f;

        private readonly List<HitStopEntry> _entries = new List<HitStopEntry>(8);
        private float _savedTimeScale = 1f;
        private float _savedFixedDeltaTime = BaseFixedDeltaTime;
        private bool _capturedBaseline;

        private struct HitStopEntry
        {
            public int token;
            public float endRealtime;
            public CombatHitStopSpec spec;
            public Animator animator;
            public float savedAnimatorSpeed;
        }

        public int Push(int token, CombatHitStopSpec spec, Animator attackerAnimator)
        {
            if (!spec.enabled || spec.durationRealSeconds <= 0f)
                return token;

            float end = Time.unscaledTime + spec.durationRealSeconds;
            bool useGlobal = spec.mode == CombatHitStopMode.GlobalTimeScale || spec.mode == CombatHitStopMode.Both;
            bool useAnimator = spec.mode == CombatHitStopMode.AttackerAnimatorOnly || spec.mode == CombatHitStopMode.Both;

            if (useGlobal)
            {
                CaptureBaselineIfNeeded();
                float scale = Mathf.Clamp(spec.timeScale, 0.01f, 1f);
                Time.timeScale = Mathf.Min(Time.timeScale, scale);
                Time.fixedDeltaTime = BaseFixedDeltaTime * Mathf.Max(Time.timeScale, 0.0001f);
            }

            float animSpeed = 1f;
            if (useAnimator && attackerAnimator != null)
            {
                animSpeed = attackerAnimator.speed;
                attackerAnimator.speed = 0f;
            }

            _entries.Add(new HitStopEntry
            {
                token = token,
                endRealtime = end,
                spec = spec,
                animator = useAnimator ? attackerAnimator : null,
                savedAnimatorSpeed = animSpeed
            });

            return token;
        }

        public void CancelToken(int token)
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].token == token)
                {
                    RestoreEntry(_entries[i]);
                    _entries.RemoveAt(i);
                }
            }

            RefreshGlobalTimeScale();
        }

        public void CancelAll()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
                RestoreEntry(_entries[i]);
            _entries.Clear();
            RestoreGlobalBaseline();
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (now >= _entries[i].endRealtime)
                {
                    RestoreEntry(_entries[i]);
                    _entries.RemoveAt(i);
                }
            }

            RefreshGlobalTimeScale();
        }

        private void CaptureBaselineIfNeeded()
        {
            if (_capturedBaseline)
                return;

            _savedTimeScale = Time.timeScale;
            _savedFixedDeltaTime = Time.fixedDeltaTime;
            _capturedBaseline = true;
        }

        private void RestoreEntry(HitStopEntry entry)
        {
            if (entry.animator != null)
                entry.animator.speed = entry.savedAnimatorSpeed;
        }

        private void RefreshGlobalTimeScale()
        {
            bool needGlobal = false;
            float minScale = 1f;
            for (int i = 0; i < _entries.Count; i++)
            {
                CombatHitStopMode mode = _entries[i].spec.mode;
                if (mode == CombatHitStopMode.GlobalTimeScale || mode == CombatHitStopMode.Both)
                {
                    needGlobal = true;
                    minScale = Mathf.Min(minScale, Mathf.Clamp(_entries[i].spec.timeScale, 0.01f, 1f));
                }
            }

            if (!needGlobal)
            {
                RestoreGlobalBaseline();
                return;
            }

            CaptureBaselineIfNeeded();
            Time.timeScale = minScale;
            Time.fixedDeltaTime = BaseFixedDeltaTime * Mathf.Max(Time.timeScale, 0.0001f);
        }

        private void RestoreGlobalBaseline()
        {
            if (!_capturedBaseline)
                return;

            Time.timeScale = _savedTimeScale;
            Time.fixedDeltaTime = _savedFixedDeltaTime;
            _capturedBaseline = false;
        }

        public static CombatHitStopService FindOrCreateOn(GameObject combatRoot)
        {
            if (combatRoot == null)
                return null;

            CombatHitStopService service = combatRoot.GetComponent<CombatHitStopService>();
            if (service == null)
                service = combatRoot.AddComponent<CombatHitStopService>();
            return service;
        }
    }
}
