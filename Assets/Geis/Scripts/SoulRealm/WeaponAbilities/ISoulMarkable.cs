using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Enemies or props that can be tagged in the soul realm (Harp-Bow Soul Marking).
    /// </summary>
    public interface ISoulMarkable
    {
        Transform MarkTransform { get; }
        bool IsSoulMarked { get; }
        void ApplySoulMark();
        void ClearSoulMark();
    }
}
