using UnityEngine;

namespace Geis.Attributes
{
    /// <summary>
    /// Groups consecutive serialized fields under a collapsible foldout in the Inspector.
    /// Apply the same <paramref name="label"/> to each field in the group (in order).
    /// </summary>
    public sealed class FoldoutGroupAttribute : PropertyAttribute
    {
        public readonly string Label;
        public readonly bool DefaultExpanded;

        public FoldoutGroupAttribute(string label, bool defaultExpanded = true)
        {
            Label = label;
            DefaultExpanded = defaultExpanded;
            // Must be higher than Tooltip/Range/etc. so this PropertyDrawer wins when multiple attributes share a field.
            order = 10000;
        }
    }
}
