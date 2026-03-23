// Geis of Anam - Resolves combo data per weapon slot.
// Maps EquippedWeaponIndex (0=Unarmed, 1=Knife, 2=Sword, 3=Bow) to GeisComboData.

using UnityEngine;

namespace Geis.Combat
{
    /// <summary>
    /// Maps weapon slots to combo data. One GeisComboData per weapon.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponComboData_", menuName = "Geis/Combat/Weapon Combo Data")]
    public class GeisWeaponComboData : ScriptableObject
    {
        [Tooltip("Combo data per weapon: [0]=Unarmed, [1]=Knife, [2]=Sword, [3]=Bow")]
        [SerializeField]
        private GeisComboData[] weaponCombos = new GeisComboData[4];

        /// <summary>
        /// Get combo data for the given weapon index (0-3). Returns null if out of range or unassigned.
        /// </summary>
        public GeisComboData GetComboForWeapon(int weaponIndex)
        {
            if (weaponCombos == null || weaponIndex < 0 || weaponIndex >= weaponCombos.Length)
                return null;
            return weaponCombos[weaponIndex];
        }
    }
}
