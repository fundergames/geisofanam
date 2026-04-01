using System.Collections;
using System.Collections.Generic;
using RogueDeal.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Geis.Puzzles
{
    /// <summary>
    /// Wires puzzle triggers to puzzle outputs. Place on an empty GameObject in a puzzle area
    /// and assign triggers and outputs in the Inspector.
    ///
    /// Logic modes:
    ///   AllRequired  — every trigger must be simultaneously active.
    ///                  Optional <see cref="timedWindowSeconds"/> (if &gt; 0): all must be active within one window from first activation.
    ///   AnyOne       — any single trigger being active solves the puzzle.
    ///   Sequence     — triggers must activate in array order; wrong order resets.
    ///                  Optional <see cref="timedWindowSeconds"/> or legacy <see cref="sequenceTimeLimitSeconds"/>:
    ///                  one window from the first correct step to finish the sequence.
    ///   Timed        — all triggers must activate within the time window; if both time fields are 0, defaults to 3s.
    /// </summary>
    public class PuzzleGroup : MonoBehaviour
    {
        public enum LogicMode { AllRequired, AnyOne, Sequence, Timed }

        [Header("Puzzle Logic")]
        [SerializeField] private PuzzleTriggerBase[] triggers;
        [SerializeField] private PuzzleOutputBase[]  outputs;
        [SerializeField] private LogicMode           logicMode = LogicMode.AllRequired;
        [Tooltip("If true, the puzzle stays solved after the first solve and trigger releases no longer call Deactivate on outputs. Use false for hold-to-activate puzzles (e.g. pressure plates that must release).")]
        [SerializeField] private bool                oneShot = true;
        [Tooltip("If > 0: one time window from the first activation/step (AllRequired, Sequence). Timed uses this, or 3s when 0. " +
                 "Sequence also falls back to sequenceTimeLimitSeconds when this is 0.")]
        [SerializeField] private float               timedWindowSeconds = 0f;
        [Tooltip("Legacy: used only when timedWindowSeconds is 0. Prefer timedWindowSeconds for new setups.")]
        [SerializeField] private float               sequenceTimeLimitSeconds = 0f;

        [Header("Events")]
        [SerializeField] private UnityEvent onSolved;
        [SerializeField] private UnityEvent onReset;

        public bool IsSolved { get; private set; }

        /// <summary>Sequence / legacy: primary <see cref="timedWindowSeconds"/>; else <see cref="sequenceTimeLimitSeconds"/>.</summary>
        private float TimeLimitForSequence()
        {
            if (timedWindowSeconds > 0f)
                return timedWindowSeconds;
            return sequenceTimeLimitSeconds;
        }

        /// <summary>AllRequired: only <see cref="timedWindowSeconds"/> (no legacy fallback).</summary>
        private float TimeLimitForAllRequired() => timedWindowSeconds > 0f ? timedWindowSeconds : 0f;

        /// <summary>Timed: explicit values, else 3s default so existing Timed puzzles keep a window.</summary>
        private float TimeLimitForTimedMode()
        {
            if (timedWindowSeconds > 0f)
                return timedWindowSeconds;
            if (sequenceTimeLimitSeconds > 0f)
                return sequenceTimeLimitSeconds;
            return 3f;
        }

        // Sequence tracking
        private int _sequenceProgress;
        private float _sequenceDeadline;

        // Timed tracking
        private readonly List<PuzzleTriggerBase> _timedActive = new List<PuzzleTriggerBase>();
        private float _timedWindowDeadline;

        // AllRequired + time window
        private bool _allRequiredWindowStarted;
        private float _allRequiredDeadline;

        private void OnEnable()
        {
            foreach (var t in triggers)
            {
                if (t == null) continue;
                t.OnTriggerActivated   += HandleActivated;
                t.OnTriggerDeactivated += HandleDeactivated;
            }
            if (Application.isPlaying)
                StartCoroutine(CoEvaluateInitialStateAfterFrame());
        }

        private void OnDisable()
        {
            foreach (var t in triggers)
            {
                if (t == null) continue;
                t.OnTriggerActivated   -= HandleActivated;
                t.OnTriggerDeactivated -= HandleDeactivated;
            }
        }

        /// <summary>
        /// Catches triggers already active before we subscribed, or composite triggers that synced
        /// before PuzzleGroup.OnEnable (script order). AnyOne / AllRequired / Timed.
        /// </summary>
        private IEnumerator CoEvaluateInitialStateAfterFrame()
        {
            yield return null;
            if (!isActiveAndEnabled) yield break;
            if (IsSolved && oneShot) yield break;

            switch (logicMode)
            {
                case LogicMode.AnyOne:
                    if (triggers == null) yield break;
                    foreach (var t in triggers)
                    {
                        if (t != null && t.IsActivated)
                        {
                            Solve();
                            yield break;
                        }
                    }
                    break;
                case LogicMode.AllRequired:
                    EvaluateAllRequired();
                    break;
                case LogicMode.Timed:
                    if (AllNonNullTriggersAreActivated())
                    {
                        _timedActive.Clear();
                        Solve();
                    }
                    break;
            }
        }

        private void Update()
        {
            if (IsSolved)
                return;

            switch (logicMode)
            {
                case LogicMode.Timed when _timedActive.Count > 0:
                {
                    float limit = TimeLimitForTimedMode();
                    if (limit > 0f && Time.time > _timedWindowDeadline)
                        ResetPuzzle();
                    break;
                }
                case LogicMode.Sequence when _sequenceProgress > 0:
                {
                    float limit = TimeLimitForSequence();
                    if (limit > 0f && Time.time > _sequenceDeadline)
                        ResetSequenceTimedOut();
                    break;
                }
                case LogicMode.AllRequired when _allRequiredWindowStarted:
                {
                    float limit = TimeLimitForAllRequired();
                    if (limit > 0f && Time.time > _allRequiredDeadline)
                        ResetAllRequiredTimedOut();
                    break;
                }
            }
        }

        private void HandleActivated(PuzzleTriggerBase trigger)
        {
            EventBus<PuzzleElementActivatedEvent>.Raise(new PuzzleElementActivatedEvent
                { Trigger = trigger, Activated = true });

            if (IsSolved && oneShot) return;

            switch (logicMode)
            {
                case LogicMode.AllRequired: EvaluateAllRequired(); break;
                case LogicMode.AnyOne:      Solve();               break;
                case LogicMode.Sequence:    EvaluateSequence(trigger); break;
                case LogicMode.Timed:       EvaluateTimed(trigger); break;
            }
        }

        private void HandleDeactivated(PuzzleTriggerBase trigger)
        {
            EventBus<PuzzleElementActivatedEvent>.Raise(new PuzzleElementActivatedEvent
                { Trigger = trigger, Activated = false });

            if (IsSolved && oneShot) return;

            switch (logicMode)
            {
                case LogicMode.AllRequired:
                {
                    if (TimeLimitForAllRequired() > 0f && !IsSolved)
                    {
                        bool anyTriggerStillHeld = false;
                        if (triggers != null)
                        {
                            foreach (var t in triggers)
                            {
                                if (t != null && t.IsActivated)
                                {
                                    anyTriggerStillHeld = true;
                                    break;
                                }
                            }
                        }
                        if (!anyTriggerStillHeld)
                        {
                            _allRequiredWindowStarted = false;
                            _allRequiredDeadline = 0f;
                        }
                    }
                    if (IsSolved)
                    {
                        IsSolved = false;
                        DeactivateOutputs();
                        EventBus<PuzzleResetEvent>.Raise(new PuzzleResetEvent { Group = this });
                        onReset?.Invoke();
                    }
                    break;
                }

                case LogicMode.Sequence:
                    // Any deactivation during sequence resets progress.
                    ResetSequence();
                    break;

                case LogicMode.Timed:
                    _timedActive.Remove(trigger);
                    break;

                case LogicMode.AnyOne:
                    if (!IsSolved || oneShot) break;
                    bool anyStillActive = false;
                    if (triggers != null)
                    {
                        foreach (var t in triggers)
                        {
                            if (t != null && t.IsActivated)
                            {
                                anyStillActive = true;
                                break;
                            }
                        }
                    }
                    if (!anyStillActive)
                    {
                        IsSolved = false;
                        DeactivateOutputs();
                        EventBus<PuzzleResetEvent>.Raise(new PuzzleResetEvent { Group = this });
                        onReset?.Invoke();
                    }
                    break;
            }
        }

        // ── Logic evaluators ────────────────────────────────────────────────────

        private void EvaluateAllRequired()
        {
            if (TimeLimitForAllRequired() > 0f && !_allRequiredWindowStarted)
            {
                bool anyActive = false;
                if (triggers != null)
                {
                    foreach (var t in triggers)
                    {
                        if (t != null && t.IsActivated)
                        {
                            anyActive = true;
                            break;
                        }
                    }
                }
                if (anyActive)
                {
                    _allRequiredWindowStarted = true;
                    _allRequiredDeadline = Time.time + TimeLimitForAllRequired();
                }
            }

            foreach (var t in triggers)
            {
                if (t != null && !t.IsActivated) return;
            }
            Solve();
        }

        private void EvaluateSequence(PuzzleTriggerBase trigger)
        {
            if (_sequenceProgress >= triggers.Length)
                return;

            if (trigger == triggers[_sequenceProgress])
            {
                _sequenceProgress++;
                float limit = TimeLimitForSequence();
                if (_sequenceProgress == 1 && limit > 0f)
                    _sequenceDeadline = Time.time + limit;
                if (_sequenceProgress >= triggers.Length)
                    Solve();
            }
            else
            {
                // Wrong order — reset
                ResetSequence();
            }
        }

        private void EvaluateTimed(PuzzleTriggerBase trigger)
        {
            // Window starts on first activation in this attempt. Triggers that were already active
            // (no transition event) must still count — otherwise e.g. standing on plate A then
            // hitting B never records A in _timedActive.
            if (_timedActive.Count == 0)
            {
                float limit = TimeLimitForTimedMode();
                if (limit > 0f)
                    _timedWindowDeadline = Time.time + limit;
                SeedTimedActiveWithAlreadyActiveTriggers();
            }

            if (!_timedActive.Contains(trigger))
                _timedActive.Add(trigger);

            if (AllNonNullTriggersRecordedInTimedWindow())
            {
                _timedActive.Clear();
                Solve();
            }
        }

        private void SeedTimedActiveWithAlreadyActiveTriggers()
        {
            if (triggers == null) return;
            foreach (var t in triggers)
            {
                if (t != null && t.IsActivated && !_timedActive.Contains(t))
                    _timedActive.Add(t);
            }
        }

        private bool AllNonNullTriggersRecordedInTimedWindow()
        {
            if (triggers == null) return false;
            foreach (var t in triggers)
            {
                if (t == null) continue;
                if (!_timedActive.Contains(t))
                    return false;
            }
            return true;
        }

        private bool AllNonNullTriggersAreActivated()
        {
            if (triggers == null) return false;
            bool any = false;
            foreach (var t in triggers)
            {
                if (t == null) continue;
                any = true;
                if (!t.IsActivated)
                    return false;
            }
            return any;
        }

        // ── State changes ────────────────────────────────────────────────────────

        private void Solve()
        {
            if (IsSolved && oneShot) return;
            IsSolved = true;
            ActivateOutputs();
            EventBus<PuzzleSolvedEvent>.Raise(new PuzzleSolvedEvent { Group = this });
            onSolved?.Invoke();
            Debug.Log($"[PuzzleGroup] '{name}' solved.");
        }

        private void ResetPuzzle()
        {
            if (IsSolved && oneShot) return;
            IsSolved = false;
            _timedActive.Clear();
            ResetSequence();
            DeactivateOutputs();
            EventBus<PuzzleResetEvent>.Raise(new PuzzleResetEvent { Group = this });
            onReset?.Invoke();
            Debug.Log($"[PuzzleGroup] '{name}' reset.");
        }

        private void ResetSequence()
        {
            _sequenceProgress = 0;
            _sequenceDeadline = 0f;
            _allRequiredWindowStarted = false;
            _allRequiredDeadline = 0f;
            _timedWindowDeadline = 0f;
            foreach (var t in triggers)
                t?.ResetSilent();
        }

        private void ResetSequenceTimedOut()
        {
            if (IsSolved && oneShot) return;
            IsSolved = false;
            ResetSequence();
            DeactivateOutputs();
            EventBus<PuzzleResetEvent>.Raise(new PuzzleResetEvent { Group = this });
            onReset?.Invoke();
            Debug.Log($"[PuzzleGroup] '{name}' sequence time limit expired; progress reset.");
        }

        private void ResetAllRequiredTimedOut()
        {
            if (IsSolved && oneShot) return;
            IsSolved = false;
            ResetSequence();
            DeactivateOutputs();
            EventBus<PuzzleResetEvent>.Raise(new PuzzleResetEvent { Group = this });
            onReset?.Invoke();
            Debug.Log($"[PuzzleGroup] '{name}' all-required time limit expired; progress reset.");
        }

        private void ActivateOutputs()
        {
            foreach (var o in outputs)
                o?.Activate();
        }

        private void DeactivateOutputs()
        {
            foreach (var o in outputs)
                o?.Deactivate();
        }
    }
}
