using UnityEngine;

namespace Geis.SoulRealm
{
    /// <summary>
    /// Optional puzzle / interact scripts can check this before running physical-realm logic.
    /// Soul-only triggers should require <see cref="SoulRealmManager.IsSoulRealmActive"/>.
    /// </summary>
    public static class SoulRealmInteractable
    {
        /// <summary>True when the player should not use normal world interactions (combat, use, pickups).</summary>
        public static bool BlockPhysicalInteractions =>
            SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive;
    }
}
