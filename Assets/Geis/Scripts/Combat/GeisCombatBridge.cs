// Geis of Anam - Bridges GeisPlayerAnimationController to RogueDeal combat (damage, hit detection).
// Add alongside GeisPlayerAnimationController to enable damage on attacks.
// Uses GeisWeaponDefinition (unified) when available; falls back to legacy arrays.

using UnityEngine;
using Geis.Locomotion;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;

namespace Geis.Combat
{
    /// <summary>
    /// Connects Geis player to RogueDeal combat. When GeisWeaponSwitcher uses unified mode,
    /// reads everything from GeisWeaponDefinition. Otherwise uses legacy combatActionsByWeapon/weaponsBySlot.
    /// </summary>
    [RequireComponent(typeof(CombatEntity))]
    [RequireComponent(typeof(RogueDeal.Combat.Presentation.CombatExecutor))]
    [RequireComponent(typeof(SimpleAttackHitDetector))]
    public class GeisCombatBridge : MonoBehaviour
    {
        [Header("Unified Mode (preferred)")]
        [Tooltip("When GeisWeaponSwitcher uses unified weapons, this is ignored. Set useUnifiedWeapons on switcher.")]
        [SerializeField] private GeisWeaponSwitcher _weaponSwitcher;

        [Header("Legacy Overrides (when switcher not unified)")]
        [Tooltip("Index matches slots: [0]=Unarmed, [1]=Knife, [2]=Sword, [3]=Bow")]
        [SerializeField] private CombatAction[] combatActionsByWeapon = new CombatAction[4];
        [SerializeField] private Weapon[] weaponsBySlot = new Weapon[4];

        [Header("References")]
        [SerializeField] private GeisPlayerAnimationController _geisController;
        [Tooltip("Log when attacks are received (for debugging)")]
        [SerializeField] private bool _debugLog = false;

        private CombatEntity _combatEntity;
        private RogueDeal.Combat.Presentation.CombatExecutor _executor;
        private SimpleAttackHitDetector _hitDetector;

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>();
            _executor = GetComponent<RogueDeal.Combat.Presentation.CombatExecutor>();
            _hitDetector = GetComponent<SimpleAttackHitDetector>();

            if (_geisController == null)
                _geisController = GetComponent<GeisPlayerAnimationController>();
            if (_weaponSwitcher == null)
                _weaponSwitcher = GetComponent<GeisWeaponSwitcher>();
        }

        private void OnEnable()
        {
            if (_geisController != null)
                _geisController.OnAttackPerformed += HandleAttackPerformed;
        }

        private void OnDisable()
        {
            if (_geisController != null)
                _geisController.OnAttackPerformed -= HandleAttackPerformed;
        }

        private void HandleAttackPerformed(int weaponIndex)
        {
            if (_hitDetector == null || _combatEntity == null || _executor == null)
                return;

            CombatAction action = null;
            Weapon weapon = null;

            var def = _weaponSwitcher != null ? _weaponSwitcher.GetWeaponDefinition(weaponIndex) : null;
            if (def != null)
            {
                action = def.GetCombatAction();
                weapon = def.GetWeaponForDamage();
            }

            if (action == null)
            {
                action = GetLegacyCombatAction(weaponIndex);
                weapon = GetLegacyWeapon(weaponIndex);
            }

            if (action == null)
            {
                if (_debugLog)
                    Debug.Log("[GeisCombatBridge] No combat action for weaponIndex " + weaponIndex + ", skipping hit check");
                return;
            }

            if (_debugLog)
                Debug.Log($"[GeisCombatBridge] HandleAttackPerformed weaponIndex={weaponIndex} action={action.actionName}");

            var entityData = _combatEntity.GetEntityData();
            if (entityData != null)
                entityData.equippedWeapon = weapon;

            _hitDetector.PerformHitCheck(action);
        }

        private CombatAction GetLegacyCombatAction(int weaponIndex)
        {
            if (combatActionsByWeapon == null) return null;
            if (weaponIndex >= 0 && weaponIndex < combatActionsByWeapon.Length && combatActionsByWeapon[weaponIndex] != null)
                return combatActionsByWeapon[weaponIndex];
            foreach (var a in combatActionsByWeapon)
            {
                if (a != null) return a;
            }
            return null;
        }

        private Weapon GetLegacyWeapon(int weaponIndex)
        {
            if (weaponsBySlot == null || weaponIndex < 0 || weaponIndex >= weaponsBySlot.Length)
                return null;
            return weaponsBySlot[weaponIndex];
        }
    }
}
