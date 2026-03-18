using System;
using System.Collections.Generic;
using UnityEngine;

namespace Funder.Core.FSM
{
    public class StateMachine
    {
        private readonly Dictionary<Type, StateNode> _states = new Dictionary<Type, StateNode>();
        private readonly Dictionary<Type, HashSet<Type>> _transitions = new Dictionary<Type, HashSet<Type>>();

        public StateNode Current { get; private set; }

        public void AddState(StateNode state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Type stateType = state.GetType();
            _states[stateType] = state;

            if (!_transitions.ContainsKey(stateType))
            {
                _transitions[stateType] = new HashSet<Type>();
            }
        }

        public void AddTransition(Type from, Type to)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            if (!_transitions.TryGetValue(from, out HashSet<Type> targets))
            {
                targets = new HashSet<Type>();
                _transitions[from] = targets;
            }

            targets.Add(to);
        }

        public bool TryGo<T>() where T : StateNode
        {
            return TryGo(typeof(T));
        }

        public bool TryGo(Type targetType)
        {
            if (targetType == null)
            {
                return false;
            }

            if (!_states.TryGetValue(targetType, out StateNode next))
            {
                Debug.LogWarning($"[StateMachine] Cannot transition to unregistered state: {targetType.Name}");
                return false;
            }

            if (Current != null)
            {
                Type currentType = Current.GetType();

                if (currentType == targetType)
                {
                    return true;
                }

                if (!_transitions.TryGetValue(currentType, out HashSet<Type> allowed) || !allowed.Contains(targetType))
                {
                    Debug.LogWarning($"[StateMachine] Invalid transition: {currentType.Name} -> {targetType.Name}");
                    return false;
                }

                Current.OnExit();
            }

            Current = next;
            Current.OnEnter();
            return true;
        }

        public void Tick(float dt)
        {
            Current?.OnTick(dt);
        }

        public void Reset()
        {
            Current?.OnExit();
            Current = null;
        }
    }
}
