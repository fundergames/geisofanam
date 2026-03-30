using Geis.Combat;
using RogueDeal.Combat;
using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Passive Lyre Sword resonance: fills when the player deals damage with a weapon whose
    /// GeisWeaponDefinition.BuildsLyreResonance is enabled.
    /// Wave Release reads Current / TryConsume.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LyreResonanceMeter : MonoBehaviour
    {
        [SerializeField] private GeisWeaponSwitcher weaponSwitcher;
        [SerializeField] private CombatEntity combatEntity;
        [Tooltip("Maximum stored resonance (wave cost should be less than or equal).")]
        [SerializeField] private float maxResonance = 100f;
        [Tooltip("Resonance gained per point of damage dealt (after armor, etc.).")]
        [SerializeField] private float resonancePerDamagePoint = 1f;

        private float _current;

        public float Current => _current;
        public float Max => maxResonance;
        public float Normalized => maxResonance > 0f ? _current / maxResonance : 0f;

        private void Awake()
        {
            if (weaponSwitcher == null)
                weaponSwitcher = GetComponent<GeisWeaponSwitcher>() ?? GetComponentInParent<GeisWeaponSwitcher>();
            if (combatEntity == null)
                combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
        }

        private void OnEnable()
        {
            CombatEvents.OnDamageApplied += OnDamageApplied;
        }

        private void OnDisable()
        {
            CombatEvents.OnDamageApplied -= OnDamageApplied;
        }

        private void OnDamageApplied(CombatEventData data)
        {
            if (combatEntity == null || data.source != combatEntity)
                return;
            if (data.damageAmount <= 0f)
                return;
            if (weaponSwitcher == null)
                return;

            var def = weaponSwitcher.GetWeaponDefinition(weaponSwitcher.CurrentWeaponIndex);
            if (def == null || !def.BuildsLyreResonance)
                return;

            _current = Mathf.Min(maxResonance, _current + data.damageAmount * resonancePerDamagePoint);
        }

        /// <summary>Returns true if at least <paramref name="amount"/> was subtracted (full or partial).</summary>
        public bool TryConsume(float amount)
        {
            if (amount <= 0f)
                return true;
            if (_current < amount)
                return false;
            _current -= amount;
            return true;
        }

        public void SetForTests(float value)
        {
            _current = Mathf.Clamp(value, 0f, maxResonance);
        }
    }
}
