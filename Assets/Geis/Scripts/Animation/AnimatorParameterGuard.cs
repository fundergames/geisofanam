using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geis.Animation
{
    /// <summary>
    /// Cached lookups for Animator parameters; invalidates when <see cref="Animator.runtimeAnimatorController"/> changes.
    /// </summary>
    public static class AnimatorParameterGuard
    {
        private sealed class CacheEntry
        {
            public RuntimeAnimatorController Controller;
            public Dictionary<string, AnimatorControllerParameterType> TypesByName;
        }

        private static readonly Dictionary<int, CacheEntry> Cache = new Dictionary<int, CacheEntry>();

        private static CacheEntry GetOrBuild(Animator animator)
        {
            if (animator == null)
                return null;

            int id = animator.GetInstanceID();
            var ctrl = animator.runtimeAnimatorController;

            if (Cache.TryGetValue(id, out var entry) && entry.Controller == ctrl)
                return entry;

            var dict = new Dictionary<string, AnimatorControllerParameterType>(StringComparer.Ordinal);
            if (ctrl != null)
            {
                foreach (var p in animator.parameters)
                    dict[p.name] = p.type;
            }

            entry = new CacheEntry { Controller = ctrl, TypesByName = dict };
            Cache[id] = entry;
            return entry;
        }

        public static bool HasParameter(Animator animator, string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            var entry = GetOrBuild(animator);
            return entry != null && entry.TypesByName != null && entry.TypesByName.ContainsKey(name);
        }

        public static bool HasTrigger(Animator animator, string name)
        {
            var entry = GetOrBuild(animator);
            if (entry?.TypesByName == null || !entry.TypesByName.TryGetValue(name, out var t))
                return false;
            return t == AnimatorControllerParameterType.Trigger;
        }

        public static bool HasParameterOfType(Animator animator, string name, AnimatorControllerParameterType type)
        {
            var entry = GetOrBuild(animator);
            return entry?.TypesByName != null
                   && entry.TypesByName.TryGetValue(name, out var t)
                   && t == type;
        }

        /// <summary>
        /// Sets the trigger only if a matching trigger parameter exists. Returns whether it was set.
        /// </summary>
        public static bool TrySetTrigger(Animator animator, string name)
        {
            if (animator == null || string.IsNullOrEmpty(name))
                return false;
            if (!HasTrigger(animator, name))
                return false;
            animator.SetTrigger(name);
            return true;
        }

        public static string FormatParameterList(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return "None (no controller)";

            var parts = new List<string>();
            foreach (var p in animator.parameters)
                parts.Add($"{p.name} ({p.type})");
            return string.Join(", ", parts);
        }
    }
}
