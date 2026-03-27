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
    ///   AnyOne       — any single trigger being active solves the puzzle.
    ///   Sequence     — triggers must activate in array order; wrong order resets.
    ///   Timed        — all triggers must activate within <see cref="timedWindowSeconds"/>.
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
        [Tooltip("(Timed mode) All triggers must fire within this many seconds of the first.")]
        [SerializeField] private float               timedWindowSeconds = 3f;

        [Header("Events")]
        [SerializeField] private UnityEvent onSolved;
        [SerializeField] private UnityEvent onReset;

        public bool IsSolved { get; private set; }

        // Sequence tracking
        private int _sequenceProgress;

        // Timed tracking
        private readonly List<PuzzleTriggerBase> _timedActive = new List<PuzzleTriggerBase>();
        private float _timedWindowStart;

        private void OnEnable()
        {
            foreach (var t in triggers)
            {
                if (t == null) continue;
                t.OnTriggerActivated   += HandleActivated;
                t.OnTriggerDeactivated += HandleDeactivated;
            }
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

        private void Update()
        {
            if (logicMode == LogicMode.Timed && _timedActive.Count > 0)
            {
                if (Time.time - _timedWindowStart > timedWindowSeconds)
                    ResetPuzzle();
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
                    // If the puzzle was solved and isn't oneShot, deactivating a trigger un-solves it.
                    if (IsSolved)
                    {
                        IsSolved = false;
                        DeactivateOutputs();
                        EventBus<PuzzleResetEvent>.Raise(new PuzzleResetEvent { Group = this });
                        onReset?.Invoke();
                    }
                    break;

                case LogicMode.Sequence:
                    // Any deactivation during sequence resets progress.
                    ResetSequence();
                    break;

                case LogicMode.Timed:
                    _timedActive.Remove(trigger);
                    break;
            }
        }

        // ── Logic evaluators ────────────────────────────────────────────────────

        private void EvaluateAllRequired()
        {
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
            if (_timedActive.Count == 0)
                _timedWindowStart = Time.time;

            if (!_timedActive.Contains(trigger))
                _timedActive.Add(trigger);

            // Check if all are now active within the window
            bool allActive = true;
            foreach (var t in triggers)
            {
                if (t != null && !_timedActive.Contains(t))
                {
                    allActive = false;
                    break;
                }
            }

            if (allActive)
            {
                _timedActive.Clear();
                Solve();
            }
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
            foreach (var t in triggers)
                t?.ResetSilent();
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
