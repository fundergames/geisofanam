using System;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;
using UnityEngine;

namespace Geis.Puzzles
{
    /// <summary>
    /// Sword (melee) puzzle trigger. Activates after <see cref="hitsRequired"/> hit windows from
    /// <see cref="SimpleAttackHitDetector"/> overlap this zone (same spheres/radius as combat melee).
    /// Optional legacy: <see cref="legacyWeaponHitboxCollider"/> + <c>WeaponHitbox</c> trigger overlap
    /// (cannot filter by <see cref="CombatAction"/>; use SimpleAttack path for ability-specific puzzles).
    ///
    /// Default realm: PhysicalOnly (see <see cref="Reset"/>).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SwordHitTrigger : PuzzleTriggerBase, IPuzzleMeleeHitSink
    {
        [Header("Hit Settings")]
        [Tooltip("Number of melee hit windows required to activate.")]
        [SerializeField] private int hitsRequired = 1;

        [Header("Weapon filter (Geis slots)")]
        [Tooltip("0=Unarmed, 1=Knife, 2=Sword, 3=Bow. Empty = any slot. Default is sword only.")]
        [SerializeField] private int[] acceptedWeaponSlots = { 2 };

        [Header("Action / ability filter (CombatAction)")]
        [Tooltip("If non-empty, only hits from a CombatAction whose actionName matches (use unique names per ability in your action assets, e.g. Sword_Light vs Sword_Charged).")]
        [SerializeField] private string[] acceptedActionNames;

        [Tooltip("When matching acceptedActionNames, compare ignoring case.")]
        [SerializeField] private bool actionNameMatchIgnoreCase = true;

        [Tooltip("If non-empty, CombatAction.weaponType must be one of these (Animator routing type). Leave empty to skip.")]
        [SerializeField] private WeaponType[] acceptedWeaponTypes;

        [Header("Legacy")]
        [Tooltip("If true, also counts overlaps from RogueDeal WeaponHitbox colliders (animation-event path). Off by default so only SimpleAttackHitDetector is used.")]
        [SerializeField] private bool legacyWeaponHitboxCollider;

        [Header("Detection volume")]
        [Tooltip("If true, BoxCollider size is expanded once from a captured base so thin floor zones still overlap the melee probe spheres.")]
        [SerializeField] private bool inflateBoxColliderOnAwake = true;

        [Tooltip("Added to captured BoxCollider size (Y is split above/below center). Ignored if zero.")]
        [SerializeField] private Vector3 boxColliderInflate = new Vector3(0.15f, 0.5f, 0.15f);

        [SerializeField] private bool _boxBaseCaptured;
        [SerializeField] private Vector3 _storedBaseBoxSize;
        [SerializeField] private Vector3 _storedBaseBoxCenter;

        [Header("Visual Feedback")]
        [Tooltip("Optional: materials swapped in order as hit count increases. " +
                 "Index 0 = pristine, index 1 = first hit, etc.")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Material[] damageStageMaterials;

        [Header("Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip breakSound;
        [SerializeField] private AudioSource audioSource;

        private int _hitCount;

        /// <summary>Unity sets PhysicalOnly so sword-break zones work in the physical world (base puzzle default is SoulOnly).</summary>
        private void Reset()
        {
            realmMode = PuzzleRealmMode.PhysicalOnly;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            PuzzleBoxColliderInflate.ApplyIfNeeded(col, inflateBoxColliderOnAwake, boxColliderInflate,
                ref _boxBaseCaptured, ref _storedBaseBoxSize, ref _storedBaseBoxCenter);

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        [ContextMenu("Recapture collider base (after resizing BoxCollider)")]
        private void RecaptureColliderBase()
        {
            if (!TryGetComponent<BoxCollider>(out var box))
                return;
            _storedBaseBoxSize = box.size;
            _storedBaseBoxCenter = box.center;
            _boxBaseCaptured = true;
            PuzzleBoxColliderInflate.ApplyIfNeeded(box, inflateBoxColliderOnAwake, boxColliderInflate,
                ref _boxBaseCaptured, ref _storedBaseBoxSize, ref _storedBaseBoxCenter);
        }

        public void OnMeleeHitFromSimpleAttack(
            SimpleAttackHitDetector source,
            CombatAction action,
            int weaponSlotIndex,
            int hitWindowIndex)
        {
            TryRegisterFromSimpleAttack(action, weaponSlotIndex);
        }

        private void TryRegisterFromSimpleAttack(CombatAction action, int weaponSlotIndex)
        {
            if (IsActivated) return;
            if (!IsAccessibleInCurrentRealm()) return;
            if (!PassesWeaponSlotFilter(weaponSlotIndex))
                return;
            if (!PassesActionFilter(action))
                return;

            RegisterHitProgress();
        }

        /// <summary>
        /// True if this puzzle requires a specific <see cref="CombatAction"/>; legacy WeaponHitbox hits cannot satisfy that.
        /// </summary>
        private bool RequiresActionFilter()
        {
            return acceptedActionNames is { Length: > 0 } || acceptedWeaponTypes is { Length: > 0 };
        }

        private bool PassesActionFilter(CombatAction action)
        {
            bool nameFilter = acceptedActionNames != null && acceptedActionNames.Length > 0;
            bool typeFilter = acceptedWeaponTypes != null && acceptedWeaponTypes.Length > 0;
            if (!nameFilter && !typeFilter)
                return true;
            if (action == null)
                return false;

            if (typeFilter)
            {
                bool ok = false;
                for (int i = 0; i < acceptedWeaponTypes.Length; i++)
                {
                    if (action.weaponType == acceptedWeaponTypes[i])
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                    return false;
            }

            if (nameFilter)
            {
                if (string.IsNullOrEmpty(action.actionName))
                    return false;
                var cmp = actionNameMatchIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                bool ok = false;
                for (int i = 0; i < acceptedActionNames.Length; i++)
                {
                    var want = acceptedActionNames[i];
                    if (string.IsNullOrEmpty(want))
                        continue;
                    if (string.Equals(action.actionName, want, cmp))
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                    return false;
            }

            return true;
        }

        private bool PassesWeaponSlotFilter(int weaponSlotIndex)
        {
            if (acceptedWeaponSlots == null || acceptedWeaponSlots.Length == 0)
                return true;
            for (int i = 0; i < acceptedWeaponSlots.Length; i++)
            {
                if (acceptedWeaponSlots[i] == weaponSlotIndex)
                    return true;
            }

            return false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!legacyWeaponHitboxCollider)
                return;
            TryRegisterSwordHitLegacy(other);
        }

        private void TryRegisterSwordHitLegacy(Collider other)
        {
            if (IsActivated) return;
            if (!IsAccessibleInCurrentRealm()) return;
            if (RequiresActionFilter())
                return;
            if (other.GetComponentInParent<WeaponHitbox>() == null)
                return;

            RegisterHitProgress();
        }

        private void RegisterHitProgress()
        {
            _hitCount++;
            ApplyDamageStageVisual();

            bool isFinalHit = _hitCount >= hitsRequired;
            PlaySound(isFinalHit ? breakSound : hitSound);

            if (isFinalHit)
                SetActivated(true);
        }

        public override void ResetSilent()
        {
            base.ResetSilent();
            _hitCount = 0;
            ApplyDamageStageVisual();
        }

        private void ApplyDamageStageVisual()
        {
            if (targetRenderer == null || damageStageMaterials == null || damageStageMaterials.Length == 0)
                return;

            int index = Mathf.Clamp(_hitCount, 0, damageStageMaterials.Length - 1);
            if (damageStageMaterials[index] != null)
                targetRenderer.material = damageStageMaterials[index];
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }
    }
}
