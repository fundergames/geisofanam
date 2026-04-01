using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>Optional helper: destroy this GameObject when True Strike hits it (physical realm).</summary>
    [DisallowMultipleComponent]
    public sealed class TrueStrikeDestroyableProp : MonoBehaviour, ITrueStrikeDestroyable
    {
        public void DestroyFromTrueStrike()
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Optional helper: destroy this GameObject when Wave Release hits it (soul realm).</summary>
    [DisallowMultipleComponent]
    public sealed class SoulWaveDestroyableProp : MonoBehaviour, ISoulRealmDestroyable
    {
        public void DestroyFromSoulWave()
        {
            Destroy(gameObject);
        }
    }
}
