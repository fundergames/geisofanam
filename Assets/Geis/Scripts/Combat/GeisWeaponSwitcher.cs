/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 *
 * This software and associated documentation files are proprietary and confidential.
 * Unauthorized copying, modification, distribution, or use of this software,
 * via any medium, is strictly prohibited without explicit written permission.
 *
 * This code is provided for personal use only by authorized recipients.
 * It may not be redistributed, sublicensed, or sold in any form.
 */

// Geis of Anam - Weapon equipping via keys 1-4 (Unarmed, Knife, Sword, Bow).
// Gamepad: D-pad up cycles to the next weapon.
// Uses GeisWeaponDefinition[] as the single source for prefab, combo, and damage.

using UnityEngine;
using UnityEngine.Serialization;
using RogueDeal.Combat;
using RogueDeal.Combat.Core.Data;
using RogueDeal.Combat.Presentation;
using Geis.Locomotion;
using Geis.SoulRealm;

namespace Geis.Combat
{
    /// <summary>
    /// Switches between weapons using keys 1-4. Slot 0=Unarmed, 1=Knife, 2=Sword, 3=Bow.
    /// Controller D-pad up cycles forward through equipped slots.
    /// Assign GeisWeaponDefinition per slot (prefab + combo + damage).
    /// </summary>
    [DefaultExecutionOrder(500)]
    public class GeisWeaponSwitcher : MonoBehaviour
    {
        /// <summary>Fired after a weapon slot is equipped (includes initial spawn).</summary>
        public event System.Action<int> WeaponEquipped;
        [Header("Weapons")]
        [Tooltip("Slots: [0]=Unarmed, [1]=Knife, [2]=Sword, [3]=Bow. Prefab + combo + damage per weapon.")]
        [FormerlySerializedAs("unifiedSlots")]
        [SerializeField]
        private GeisWeaponDefinition[] weaponSlots = new GeisWeaponDefinition[4];

        [Header("Attachment")]
        [Tooltip("Optional: overrides auto-detect for the right-hand bone (Hand_R)")]
        [SerializeField]
        private Transform manualAttachmentPoint;

        [Tooltip("Optional: overrides auto-detect for the left-hand bone (Hand_L)")]
        [SerializeField]
        private Transform manualLeftAttachmentPoint;

        [Tooltip("Optional: assign Animator manually if on different branch of hierarchy")]
        [SerializeField]
        private Animator manualAnimator;

        [Tooltip("Prop socket names used only for grip offset — never as the weapon parent.")]
        [SerializeField]
        private string[] rightHandSocketNames = { "Prop_R_Socket", "Prop_R" };

        [Tooltip("Prop socket names used only for grip offset — never as the weapon parent.")]
        [SerializeField]
        private string[] leftHandSocketNames = { "Prop_L_Socket", "Prop_L" };

        [Tooltip("Hand bone names for weapon parenting (Prop_* sockets are never used as parent).")]
        [FormerlySerializedAs("attachmentBoneNames")]
        [SerializeField]
        private string[] rightHandBoneNames = { "Hand_R", "hand_r" };

        [Tooltip("Hand bone names for left-hand weapon parenting.")]
        [SerializeField]
        private string[] leftHandBoneNames = { "Hand_L", "hand_l" };

        [FormerlySerializedAs("useAnimatorRightHandFallback")]
        [SerializeField]
        private bool useAnimatorHandFallback = true;

        private Transform _rightHandAttachment;
        private Transform _leftHandAttachment;
        private Transform _rightHandSocket;
        private Transform _leftHandSocket;
        private GameObject[] _weaponInstances;
        private int _currentWeaponIndex = -1;
        private int _pendingEquipSlot = -1;
        private int _spectralArmResyncFramesRemaining;
        private Animator _animator;
        private GeisPlayerAnimationController _playerAnimationController;
        private Transform _inactiveWeaponHolder;

        /// <summary>
        /// Current weapon index (0-3). -1 if none equipped.
        /// </summary>
        public int CurrentWeaponIndex => _currentWeaponIndex;

        /// <summary>
        /// Live instantiated weapon prefab for the currently equipped slot, if any.
        /// </summary>
        public GameObject CurrentWeaponInstance =>
            _weaponInstances != null
            && _currentWeaponIndex >= 0
            && _currentWeaponIndex < _weaponInstances.Length
                ? _weaponInstances[_currentWeaponIndex]
                : null;

        /// <summary>
        /// Weapon definition for the currently equipped slot, if assigned.
        /// </summary>
        public GeisWeaponDefinition CurrentWeaponDefinition => GetWeaponDefinition(_currentWeaponIndex);

        private CombatEntity _combatEntity;

        /// <summary>
        /// Get combo data for the given weapon index. Returns definition.comboData when assigned.
        /// </summary>
        public bool TryGetComboForWeapon(int weaponIndex, out GeisComboData combo)
        {
            combo = null;
            if (weaponSlots != null && weaponIndex >= 0 && weaponIndex < weaponSlots.Length)
            {
                var def = weaponSlots[weaponIndex];
                if (def != null)
                {
                    combo = def.comboData;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the weapon definition at index, or null if out of range / unassigned.
        /// </summary>
        public GeisWeaponDefinition GetWeaponDefinition(int weaponIndex)
        {
            if (weaponSlots == null || weaponIndex < 0 || weaponIndex >= weaponSlots.Length)
                return null;
            return weaponSlots[weaponIndex];
        }

        /// <summary>
        /// Resolved hand socket used for weapon parenting in the physical realm.
        /// </summary>
        public Transform GetHandAttachmentTransform(WeaponAttachmentHand hand) =>
            ResolveHandAttachment(hand);

        /// <summary>
        /// Moves every pooled weapon off the character rig so a spectral mesh clone stays clean.
        /// </summary>
        public void DetachAllWeaponsFromCharacterRig()
        {
            if (_weaponInstances == null)
                return;

            Transform holder = EnsureInactiveWeaponHolder();
            for (int i = 0; i < _weaponInstances.Length; i++)
            {
                GameObject instance = _weaponInstances[i];
                if (instance == null)
                    continue;

                instance.transform.SetParent(holder, false);
                SnapWeaponLocalTransform(instance.transform);
            }
        }

        /// <summary>
        /// Re-parents every pooled weapon instance (e.g. after soul-realm enter/exit).
        /// </summary>
        public void RefreshAllWeaponAttachmentParents()
        {
            EnsureAnimator();
            FindAttachmentPoints();

            GeisWeaponDefinition def = GetWeaponDefinition(_currentWeaponIndex);
            ApplyEquippedWeaponIndexToAnimators(def);
            SetEmbeddedRigWeaponMeshesVisible(def == null || def.weaponPrefab == null);

            if (SoulRealmManager.Instance != null && SoulRealmManager.Instance.IsSoulRealmActive)
            {
                if (def != null && !IsBowWeapon(def))
                    FinalizeSoulRealmMeleeWeaponPose();
            }

            AssignWeaponParents(_currentWeaponIndex);
        }

        private void OnEnable()
        {
            SoulRealmManager.SoulRealmStateChanged += HandleSoulRealmStateChanged;
        }

        private void OnDisable()
        {
            SoulRealmManager.SoulRealmStateChanged -= HandleSoulRealmStateChanged;
        }

        private void HandleSoulRealmStateChanged()
        {
            RefreshAllWeaponAttachmentParents();
        }

        private void Awake()
        {
            _combatEntity = GetComponent<CombatEntity>() ?? GetComponentInParent<CombatEntity>();
            _playerAnimationController = GetComponent<GeisPlayerAnimationController>()
                ?? GetComponentInChildren<GeisPlayerAnimationController>(true)
                ?? GetComponentInParent<GeisPlayerAnimationController>();
            EnsureWeaponInstanceArray();
            EnsureAnimator();
            FindAttachmentPoints();
            PurgeUnmanagedWeaponInstances();
        }

        private void Start()
        {
            EnsureAnimator();
            if (_rightHandAttachment == null || _leftHandAttachment == null)
                FindAttachmentPoints();

            int slotCount = weaponSlots != null ? weaponSlots.Length : 0;
            for (int i = 0; i < slotCount; i++)
                EnsureWeaponInstance(i);

            if (_currentWeaponIndex < 0 && slotCount > 0)
                RequestEquipWeapon(0);
        }

        private void EnsureAnimator()
        {
            if (_animator != null)
                return;

            _animator = manualAnimator
                ?? GetComponent<Animator>()
                ?? GetComponentInChildren<Animator>(true)
                ?? GetComponentInParent<Animator>();
        }

        private void LateUpdate()
        {
            if (_pendingEquipSlot >= 0)
            {
                int slot = _pendingEquipSlot;
                _pendingEquipSlot = -1;
                EquipWeaponNow(slot);
            }

            MaintainEquippedWeaponTransform();
            ApplySpectralArmResyncIfNeeded();
        }

        /// <summary>
        /// Re-copy cleared body arm locals onto the spectral clone for a few frames after bow→melee
        /// so locomotion updates cannot restore bow-poisoned bone poses.
        /// </summary>
        private void ApplySpectralArmResyncIfNeeded()
        {
            if (_spectralArmResyncFramesRemaining <= 0)
                return;

            SoulRealmManager mgr = SoulRealmManager.Instance;
            if (mgr == null || !mgr.IsSoulRealmActive)
            {
                _spectralArmResyncFramesRemaining = 0;
                return;
            }

            GeisWeaponDefinition def = GetWeaponDefinition(_currentWeaponIndex);
            if (def == null || IsBowWeapon(def))
            {
                _spectralArmResyncFramesRemaining = 0;
                return;
            }

            mgr.SyncSpectralWeaponArmFromBody(WeaponAttachmentHand.RightHand);
            mgr.SyncSpectralWeaponArmFromBody(WeaponAttachmentHand.LeftHand);
            AssignWeaponParents(_currentWeaponIndex);
            _spectralArmResyncFramesRemaining--;
        }

        private void Update()
        {
            int slotCount = weaponSlots != null ? Mathf.Min(4, weaponSlots.Length) : 0;
            if (slotCount == 0) return;

            for (int i = 0; i < slotCount; i++)
            {
                if (GetKeyDownForSlot(i))
                {
                    RequestEquipWeapon(i);
                    return;
                }
            }

            if (WasCycleWeaponPressed())
            {
                int cur = _currentWeaponIndex < 0 ? 0 : _currentWeaponIndex;
                int next = (cur + 1) % slotCount;
                RequestEquipWeapon(next);
            }
        }

        private void RequestEquipWeapon(int slotIndex)
        {
            if (weaponSlots == null || slotIndex < 0 || slotIndex >= weaponSlots.Length)
                return;
            _pendingEquipSlot = slotIndex;
        }

        private bool GetKeyDownForSlot(int index)
        {
            if (index < 0 || index > 3) return false;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var key = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit1 + index);
                return UnityEngine.InputSystem.Keyboard.current[key].wasPressedThisFrame;
            }
#endif
            return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + index));
        }

        /// <summary>
        /// D-pad up (gamepad). Uses same gamepad resolution as <see cref="RogueDeal.Combat.CombatInputReader"/>.
        /// </summary>
        private bool WasCycleWeaponPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var gamepad = UnityEngine.InputSystem.Gamepad.current;
            if (gamepad == null && UnityEngine.InputSystem.Gamepad.all.Count > 0)
                gamepad = UnityEngine.InputSystem.Gamepad.all[0];
            if (gamepad != null && gamepad.dpad.up.wasPressedThisFrame)
                return true;
#else
            if (Input.GetKeyDown(KeyCode.JoystickButton3))
                return true;
#endif
            return false;
        }

        private void FindAttachmentPoints()
        {
            if (_animator != null)
            {
                _rightHandAttachment = FindHandBoneOnAnimator(WeaponAttachmentHand.RightHand);
                _rightHandSocket = FindHandByBoneNames(_animator, rightHandSocketNames);

                _leftHandAttachment = FindHandBoneOnAnimator(WeaponAttachmentHand.LeftHand);
                _leftHandSocket = FindHandByBoneNames(_animator, leftHandSocketNames);

                if (_rightHandAttachment == null)
                    _rightHandAttachment = _animator.transform;
                if (_leftHandAttachment == null)
                    _leftHandAttachment = _rightHandAttachment;
            }

            Transform manualRight = SanitizeManualHandOverride(manualAttachmentPoint, WeaponAttachmentHand.RightHand);
            if (manualRight != null)
                _rightHandAttachment = manualRight;
            Transform manualLeft = SanitizeManualHandOverride(manualLeftAttachmentPoint, WeaponAttachmentHand.LeftHand);
            if (manualLeft != null)
                _leftHandAttachment = manualLeft;

            if (_rightHandAttachment == null && _leftHandAttachment == null && _animator == null)
                Debug.LogWarning("[GeisWeaponSwitcher] No Animator and no manual attachment points.", this);
        }

        private Transform SanitizeManualHandOverride(Transform manual, WeaponAttachmentHand hand)
        {
            if (manual == null)
                return null;

            if (!IsPropOrSocketTransform(manual))
                return manual;

            Debug.LogWarning(
                $"[GeisWeaponSwitcher] Manual {(hand == WeaponAttachmentHand.LeftHand ? "left" : "right")}-hand " +
                $"override '{manual.name}' is a prop/socket; using Hand bone instead.",
                this);
            return null;
        }

        private Transform FindHandBoneOnAnimator(WeaponAttachmentHand hand)
        {
            if (_animator == null)
                return null;

            string[] boneNames = hand == WeaponAttachmentHand.LeftHand
                ? leftHandBoneNames
                : rightHandBoneNames;
            if (boneNames != null)
            {
                for (int i = 0; i < boneNames.Length; i++)
                {
                    if (IsPropBoneName(boneNames[i]))
                        continue;

                    Transform bone = FindTransformRecursive(_animator.transform, boneNames[i]);
                    if (bone != null && !IsPropOrSocketTransform(bone))
                        return bone;
                }
            }

            if (useAnimatorHandFallback
                && _animator.avatar != null
                && _animator.avatar.isHuman)
            {
                Transform humanoidHand = _animator.GetBoneTransform(
                    hand == WeaponAttachmentHand.LeftHand
                        ? HumanBodyBones.LeftHand
                        : HumanBodyBones.RightHand);
                if (humanoidHand != null && !IsPropOrSocketTransform(humanoidHand))
                    return humanoidHand;
            }

            return null;
        }

        private static bool IsPropBoneName(string boneName) =>
            !string.IsNullOrEmpty(boneName)
            && (boneName.Contains("Prop_") || boneName.Contains("Socket", System.StringComparison.OrdinalIgnoreCase));

        private static bool IsPropOrSocketTransform(Transform t)
        {
            if (t == null)
                return false;

            string name = t.name;
            return name.Contains("Prop_") || name.Contains("Socket", System.StringComparison.OrdinalIgnoreCase);
        }

        private Transform ResolveHandAttachment(WeaponAttachmentHand hand)
        {
            if (hand == WeaponAttachmentHand.LeftHand)
            {
                if (manualLeftAttachmentPoint != null)
                    return manualLeftAttachmentPoint;
                if (_leftHandAttachment != null)
                    return _leftHandAttachment;
            }
            else
            {
                if (manualAttachmentPoint != null)
                    return manualAttachmentPoint;
                if (_rightHandAttachment != null)
                    return _rightHandAttachment;
            }

            EnsureAnimator();
            FindAttachmentPoints();
            return hand == WeaponAttachmentHand.LeftHand ? _leftHandAttachment : _rightHandAttachment;
        }

        private Transform ResolveAttachmentParent(GeisWeaponDefinition def)
        {
            WeaponAttachmentHand hand = def != null
                ? def.AttachmentHand
                : WeaponAttachmentHand.RightHand;

            SoulRealmManager mgr = SoulRealmManager.Instance;
            if (mgr != null && mgr.IsSoulRealmActive)
            {
                if (TryResolveSpectralAttachmentParent(mgr, def, hand, out Transform spectralSocket))
                    return spectralSocket;

                Debug.LogWarning(
                    "[GeisWeaponSwitcher] Soul realm active but no spectral socket was resolved; " +
                    "hiding weapon until a spectral socket is available.",
                    this);
                return EnsureInactiveWeaponHolder();
            }

            return GetAttachmentParent(def);
        }

        private bool TryResolveSpectralAttachmentParent(
            SoulRealmManager mgr,
            GeisWeaponDefinition def,
            WeaponAttachmentHand hand,
            out Transform spectralSocket)
        {
            spectralSocket = null;
            if (mgr == null || !mgr.IsSoulRealmActive)
                return false;

            // Parent to the hand bone only; prop sockets are used for grip offset in SnapWeaponLocalTransform.
            if (mgr.TryGetLiveSpectralHand(hand, out spectralSocket))
                return true;

            spectralSocket = null;
            return false;
        }

        private void MaintainEquippedWeaponTransform()
        {
            AssignWeaponParents(_currentWeaponIndex);
        }

        private void AssignWeaponParents(int activeSlotIndex)
        {
            if (_weaponInstances == null)
                return;

            Transform hiddenParent = EnsureInactiveWeaponHolder();

            for (int i = 0; i < _weaponInstances.Length; i++)
            {
                GameObject instance = _weaponInstances[i];
                if (instance == null)
                    continue;

                Transform wantParent = hiddenParent;
                if (i == activeSlotIndex && activeSlotIndex >= 0)
                {
                    GeisWeaponDefinition def = GetWeaponDefinition(i);
                    Transform socketParent = ResolveAttachmentParent(def);
                    if (socketParent != null)
                        wantParent = socketParent;
                }

                Transform weaponTransform = instance.transform;
                if (weaponTransform.parent != wantParent)
                    weaponTransform.SetParent(wantParent, false);

                GeisWeaponDefinition slotDef = GetWeaponDefinition(i);
                SnapWeaponLocalTransform(weaponTransform, slotDef);

                if (i == activeSlotIndex
                    && activeSlotIndex >= 0
                    && SoulRealmManager.Instance != null
                    && SoulRealmManager.Instance.IsSoulRealmActive)
                    LogSoulWeaponAttach(GetWeaponDefinition(i), weaponTransform.parent);

                if (i == activeSlotIndex
                    && activeSlotIndex >= 0
                    && SoulRealmManager.Instance != null
                    && SoulRealmManager.Instance.IsSoulRealmActive
                    && !SoulRealmManager.Instance.IsTransformUnderSpectralVisual(weaponTransform))
                {
                    GeisWeaponDefinition activeDef = GetWeaponDefinition(i);
                    WeaponAttachmentHand hand = activeDef != null
                        ? activeDef.AttachmentHand
                        : WeaponAttachmentHand.RightHand;
                    if (TryResolveSpectralAttachmentParent(
                            SoulRealmManager.Instance,
                            activeDef,
                            hand,
                            out Transform spectralSocket))
                    {
                        weaponTransform.SetParent(spectralSocket, false);
                        SnapWeaponLocalTransform(weaponTransform, activeDef);
                    }
                }
            }
        }

        private Transform EnsureInactiveWeaponHolder()
        {
            if (_inactiveWeaponHolder == null)
            {
                var holderObject = new GameObject("WeaponPool_Hidden");
                holderObject.transform.SetParent(transform, false);
                _inactiveWeaponHolder = holderObject.transform;
            }

            return _inactiveWeaponHolder;
        }

        private static Transform FindHandByBoneNames(Animator animator, string[] boneNames)
        {
            if (boneNames == null)
                return null;
            foreach (var name in boneNames)
            {
                var t = FindTransformRecursive(animator.transform, name);
                if (t != null)
                    return t;
            }

            return null;
        }

        private Transform GetAttachmentParent(GeisWeaponDefinition def)
        {
            if (def != null && def.AttachmentHand == WeaponAttachmentHand.LeftHand)
            {
                Transform left = ResolveHandAttachment(WeaponAttachmentHand.LeftHand);
                if (left != null)
                    return left;
                Debug.LogWarning(
                    "[GeisWeaponSwitcher] Left-hand attachment not found; using right-hand socket.",
                    this);
            }

            Transform right = ResolveHandAttachment(WeaponAttachmentHand.RightHand);
            if (right != null)
                return right;

            Debug.LogWarning("[GeisWeaponSwitcher] Right-hand attachment not found.", this);
            return _animator != null ? _animator.transform : null;
        }

        private static Transform FindTransformRecursive(Transform parent, string name)
        {
            if (parent == null)
                return null;

            if (parent.GetComponentInParent<GeisEquippedWeaponInstanceMarker>() != null)
                return null;

            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                if (child.GetComponentInParent<GeisEquippedWeaponInstanceMarker>() != null)
                    continue;

                Transform found = FindTransformRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Re-parents pooled weapons (e.g. after soul-realm exit destroyed the spectral parent).
        /// </summary>
        public void RefreshEquippedWeaponVisual()
        {
            if (_currentWeaponIndex >= 0)
                EquipWeaponNow(_currentWeaponIndex);
        }

        /// <summary>
        /// Equip weapon at slot index (0=Unarmed, 1=Knife, 2=Sword, 3=Bow).
        /// Queues for <see cref="LateUpdate"/> so attachment runs after the animator (hit reactions, bow layers).
        /// </summary>
        public void EquipWeapon(int slotIndex)
        {
            RequestEquipWeapon(slotIndex);
        }

        private void EquipWeaponNow(int slotIndex)
        {
            if (weaponSlots == null || slotIndex < 0 || slotIndex >= weaponSlots.Length)
                return;

            EnsureAnimator();
            FindAttachmentPoints();

            GeisWeaponDefinition def = weaponSlots[slotIndex];
            bool wasBowEquipped = _currentWeaponIndex >= 0 && IsBowWeapon(GetWeaponDefinition(_currentWeaponIndex));
            if (!IsBowWeapon(def) && _playerAnimationController != null)
                _playerAnimationController.PrepareAnimatorForNonBowWeapon(reevaluateAnimator: true);

            SetAllWeaponInstancesActive(false);

            GameObject instance = EnsureWeaponInstance(slotIndex);
            AssignWeaponParents(slotIndex);

            if (instance != null)
                instance.SetActive(true);

            if (_combatEntity != null)
            {
                var data = _combatEntity.GetEntityData();
                if (data != null && def != null)
                    data.equippedWeapon = def.GetWeaponForDamage();
            }

            _currentWeaponIndex = slotIndex;

            EnsureAnimator();
            ApplyEquippedWeaponIndexToAnimators(def);
            SetEmbeddedRigWeaponMeshesVisible(def == null || def.weaponPrefab == null);

            var mgr = SoulRealmManager.Instance;
            mgr?.SyncSpectralAnimatorControllerFromBody();

            if (!IsBowWeapon(def))
                FinalizeSoulRealmMeleeWeaponPose();

            if (wasBowEquipped
                && !IsBowWeapon(def)
                && SoulRealmManager.Instance != null
                && SoulRealmManager.Instance.IsSoulRealmActive)
                _spectralArmResyncFramesRemaining = 10;

            WeaponEquipped?.Invoke(slotIndex);
        }

        /// <summary>
        /// Bow_Draw can leave Prop_R / Hand_R in a bow idle pose at layer weight 0 until the spectral
        /// animator is forced to re-evaluate. Re-parent after the pose reset so melee sits correctly.
        /// </summary>
        private void FinalizeSoulRealmMeleeWeaponPose()
        {
            SoulRealmManager mgr = SoulRealmManager.Instance;
            if (mgr == null || !mgr.IsSoulRealmActive)
                return;

            _playerAnimationController?.PrepareAnimatorForNonBowWeapon(reevaluateAnimator: true);
            mgr.ResetSpectralAndBodyMeleePose();
            AssignWeaponParents(_currentWeaponIndex);
        }

        private void EnsureWeaponInstanceArray()
        {
            int count = weaponSlots != null ? weaponSlots.Length : 0;
            if (_weaponInstances == null || _weaponInstances.Length != count)
                _weaponInstances = new GameObject[count];
        }

        private void PurgeUnmanagedWeaponInstances()
        {
            var markers = GetComponentsInChildren<GeisEquippedWeaponInstanceMarker>(true);
            foreach (var marker in markers)
            {
                if (marker == null)
                    continue;

                Destroy(marker.gameObject);
            }
        }

        private GameObject EnsureWeaponInstance(int slotIndex)
        {
            EnsureWeaponInstanceArray();
            if (slotIndex < 0 || slotIndex >= _weaponInstances.Length)
                return null;

            GameObject existing = _weaponInstances[slotIndex];
            if (existing != null)
                return existing;

            GeisWeaponDefinition def = weaponSlots[slotIndex];
            GameObject prefab = def != null ? def.weaponPrefab : null;
            if (prefab == null)
                return null;

            Transform parent = ResolveAttachmentParent(def);
            if (parent == null)
            {
                Debug.LogWarning(
                    $"[GeisWeaponSwitcher] Could not resolve attachment for slot {slotIndex} ({def.displayName}).",
                    this);
                return null;
            }

            GameObject instance = Instantiate(prefab, parent);
            SnapWeaponLocalTransform(instance.transform, def);
            instance.AddComponent<GeisEquippedWeaponInstanceMarker>();

            if (def != null && !def.IsBowWeapon)
                EnsureMeleeWeaponHitbox(instance);

            instance.SetActive(false);
            _weaponInstances[slotIndex] = instance;
            return instance;
        }

        private void SetAllWeaponInstancesActive(bool active)
        {
            if (_weaponInstances == null)
                return;

            for (int i = 0; i < _weaponInstances.Length; i++)
            {
                if (_weaponInstances[i] != null)
                    _weaponInstances[i].SetActive(active);
            }
        }

        private Transform GetHandBoneTransform(WeaponAttachmentHand hand) => ResolveHandAttachment(hand);

        private Transform GetSocketTransform(WeaponAttachmentHand hand)
        {
            EnsureAnimator();
            FindAttachmentPoints();
            return hand == WeaponAttachmentHand.LeftHand ? _leftHandSocket : _rightHandSocket;
        }

        private void SnapWeaponLocalTransform(Transform weaponTransform, GeisWeaponDefinition def = null)
        {
            weaponTransform.localScale = Vector3.one;

            if (def != null)
            {
                WeaponAttachmentHand hand = def.AttachmentHand;
                Transform handBone = GetHandBoneTransform(hand);
                Transform socket = GetSocketTransform(hand);
                if (handBone != null && socket != null)
                {
                    weaponTransform.localPosition = handBone.InverseTransformPoint(socket.position);
                    weaponTransform.localRotation =
                        Quaternion.Inverse(handBone.rotation) * socket.rotation;
                    return;
                }
            }

            weaponTransform.localPosition = Vector3.zero;
            weaponTransform.localRotation = Quaternion.identity;
        }

        private void OnDestroy()
        {
            if (_weaponInstances == null)
                return;

            for (int i = 0; i < _weaponInstances.Length; i++)
            {
                if (_weaponInstances[i] != null)
                    Destroy(_weaponInstances[i]);
            }
        }

        private static void EnsureMeleeWeaponHitbox(GameObject weaponInstance)
        {
            if (weaponInstance == null || weaponInstance.GetComponentInChildren<WeaponHitbox>(true) != null)
                return;

            Transform blade = weaponInstance.transform;
            var hitboxGo = new GameObject("WeaponHitbox");
            hitboxGo.transform.SetParent(blade, false);
            hitboxGo.transform.localPosition = new Vector3(0f, 0f, 0.45f);

            var capsule = hitboxGo.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.radius = 0.12f;
            capsule.height = 0.9f;
            capsule.direction = 2;

            var hitbox = hitboxGo.AddComponent<WeaponHitbox>();
            hitbox.targetLayers = ~0;
            hitbox.validTargetTags = new[] { "Enemy" };
        }

        private static bool IsBowWeapon(GeisWeaponDefinition def) =>
            def != null && def.IsBowWeapon;

        /// <summary>
        /// AC_Polygon EquippedWeaponIndex: 0 unarmed, 1 knife, 2 sword, 3 bow.
        /// Switcher slot order may differ (e.g. slot 2 = bow, slot 0 = Emberblade sword).
        /// </summary>
        private static int GetAnimatorEquippedWeaponIndex(GeisWeaponDefinition def)
        {
            if (def == null || def.weaponPrefab == null)
                return 0;
            if (def.IsBowWeapon)
                return 3;

            string label = (def.displayName + " " + def.name).ToLowerInvariant();
            if (label.Contains("dagger")
                || label.Contains("aether")
                || label.Contains("knife")
                || label.Contains("storm"))
                return 1;

            return 2;
        }

        private void ApplyEquippedWeaponIndexToAnimators(GeisWeaponDefinition def)
        {
            int index = GetAnimatorEquippedWeaponIndex(def);
            ApplyEquippedWeaponIndexToAnimator(_animator, index);

            SoulRealmManager mgr = SoulRealmManager.Instance;
            if (mgr != null && mgr.IsSoulRealmActive)
                ApplyEquippedWeaponIndexToAnimator(mgr.SpectralAnimator, index);
        }

        private static readonly string[] EmbeddedRigWeaponMeshNames =
        {
            "Item_Sword",
            "Item_SwordHolder",
            "Item_SwordSheath",
            "Item_Dagger"
        };

        private void SetEmbeddedRigWeaponMeshesVisible(bool visible)
        {
            SetEmbeddedRigWeaponMeshesOnRig(_animator != null ? _animator.transform : null, visible);

            SoulRealmManager mgr = SoulRealmManager.Instance;
            if (mgr != null && mgr.IsSoulRealmActive && mgr.SpectralAnimator != null)
                SetEmbeddedRigWeaponMeshesOnRig(mgr.SpectralAnimator.transform, visible);
        }

        private static void SetEmbeddedRigWeaponMeshesOnRig(Transform root, bool visible)
        {
            if (root == null)
                return;

            foreach (string meshName in EmbeddedRigWeaponMeshNames)
            {
                Transform mesh = FindTransformRecursive(root, meshName);
                if (mesh != null)
                    mesh.gameObject.SetActive(visible);
            }
        }

        private void LogSoulWeaponAttach(GeisWeaponDefinition def, Transform parent)
        {
            if (parent == null)
                return;

            bool onSpectral = SoulRealmManager.Instance != null
                && SoulRealmManager.Instance.IsTransformUnderSpectralVisual(parent);
            Debug.Log(
                $"[GeisWeaponSwitcher] Soul attach '{def?.displayName}' " +
                $"animIdx={GetAnimatorEquippedWeaponIndex(def)} " +
                $"onSpectral={onSpectral} parent={GetTransformPath(parent)}",
                this);
        }

        private static string GetTransformPath(Transform t)
        {
            if (t == null)
                return "null";

            var parts = new System.Collections.Generic.List<string>();
            for (Transform current = t; current != null; current = current.parent)
                parts.Insert(0, current.name);
            return string.Join("/", parts);
        }

        private static void ApplyEquippedWeaponIndexToAnimator(Animator anim, int animatorWeaponIndex)
        {
            if (anim == null)
                return;
            int hash = Animator.StringToHash("EquippedWeaponIndex");
            foreach (var p in anim.parameters)
            {
                if (p.name == "EquippedWeaponIndex")
                {
                    anim.SetInteger(hash, animatorWeaponIndex);
                    break;
                }
            }
        }
    }
}
