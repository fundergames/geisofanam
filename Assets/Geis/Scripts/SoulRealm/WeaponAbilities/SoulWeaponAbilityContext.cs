using Geis.Combat;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Immutable snapshot passed into a soul-weapon ability. Keeps abilities testable and free of hidden globals.
    /// </summary>
    public readonly struct SoulWeaponAbilityContext
    {
        public SoulWeaponAbilityContext(
            int weaponSlotIndex,
            GeisWeaponDefinition weaponDefinition,
            Transform owner,
            Camera viewCamera,
            Vector3 forwardWorld,
            Vector3 originWorld)
        {
            WeaponSlotIndex = weaponSlotIndex;
            WeaponDefinition = weaponDefinition;
            Owner = owner;
            ViewCamera = viewCamera;
            ForwardWorld = forwardWorld;
            OriginWorld = originWorld;
        }

        /// <summary>Slot index from <see cref="GeisWeaponSwitcher"/> (0 = unarmed, etc.).</summary>
        public int WeaponSlotIndex { get; }

        public GeisWeaponDefinition WeaponDefinition { get; }

        /// <summary>Player / ghost root for raycasts and world queries.</summary>
        public Transform Owner { get; }

        /// <summary>Optional main camera for screen-center raycasts (may be null).</summary>
        public Camera ViewCamera { get; }

        /// <summary>Horizontal-forward direction for cones and waves (typically camera yaw).</summary>
        public Vector3 ForwardWorld { get; }

        /// <summary>World origin for abilities (typically <see cref="Owner"/> position).</summary>
        public Vector3 OriginWorld { get; }
    }
}
