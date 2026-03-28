using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Requires two sub-triggers to be simultaneously active — one in the soul realm and one
    /// in the physical realm. The most interesting cross-realm puzzle: the player must leave
    /// something holding the physical trigger (or solve it first before it auto-resets) and
    /// then enter the soul realm to hit the soul trigger.
    ///
    /// Set realmMode to BothRealms so the composite trigger is always watchable.
    /// </summary>
    public class DualRealmTrigger : PuzzleTriggerBase
    {
        /// <summary>Composite is always relevant in both realms; sub-triggers carry SoulOnly / PhysicalOnly.</summary>
        public override PuzzleRealmMode RealmMode => PuzzleRealmMode.BothRealms;

        [Header("Sub-Triggers")]
        [Tooltip("Trigger that must be active in the soul realm (SoulOnly element).")]
        [SerializeField] private PuzzleTriggerBase soulTrigger;
        [Tooltip("Trigger that must be active in the physical realm (PhysicalOnly element).")]
        [SerializeField] private PuzzleTriggerBase physicalTrigger;

        private void OnEnable()
        {
            if (soulTrigger != null)
            {
                soulTrigger.OnTriggerActivated   += OnSubChanged;
                soulTrigger.OnTriggerDeactivated += OnSubChanged;
            }
            if (physicalTrigger != null)
            {
                physicalTrigger.OnTriggerActivated   += OnSubChanged;
                physicalTrigger.OnTriggerDeactivated += OnSubChanged;
            }
        }

        private void OnDisable()
        {
            if (soulTrigger != null)
            {
                soulTrigger.OnTriggerActivated   -= OnSubChanged;
                soulTrigger.OnTriggerDeactivated -= OnSubChanged;
            }
            if (physicalTrigger != null)
            {
                physicalTrigger.OnTriggerActivated   -= OnSubChanged;
                physicalTrigger.OnTriggerDeactivated -= OnSubChanged;
            }
        }

        private void OnSubChanged(PuzzleTriggerBase _)
        {
            bool bothActive = (soulTrigger     != null && soulTrigger.IsActivated) &&
                              (physicalTrigger  != null && physicalTrigger.IsActivated);
            SetActivated(bothActive);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            soulTrigger?.ResetSilent();
            physicalTrigger?.ResetSilent();
        }
    }
}
