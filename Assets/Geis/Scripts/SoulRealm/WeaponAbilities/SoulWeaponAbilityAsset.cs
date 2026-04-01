using UnityEngine;

namespace Geis.SoulRealm.WeaponAbilities
{
    /// <summary>
    /// Per-weapon soul ability hook. Subclass for concrete behavior (puzzle pulses, echoes, etc.).
    /// Data-driven: assign instances on each <see cref="GeisWeaponDefinition"/>.
    /// </summary>
    public abstract class SoulWeaponAbilityAsset : ScriptableObject
    {
        [Header("VFX (optional)")]
        [Tooltip("Spawned at the ability origin when PlayDefaultActivationVfx runs. If null, a small procedural burst is used when fallback is enabled.")]
        [SerializeField] private GameObject activationVfxPrefab;

        [SerializeField] private Vector3 activationVfxOffset = new Vector3(0f, 0.5f, 0f);

        [SerializeField] private float activationVfxLifetimeSeconds = 2.5f;

        [SerializeField] private bool rotateVfxToAbilityForward = true;

        [SerializeField] private bool proceduralBurstIfNoPrefab = true;

        [SerializeField] private Color proceduralBurstColor = new Color(0.35f, 0.9f, 1f, 1f);

        /// <summary>Short label for UI / logs (optional).</summary>
        public virtual string AbilityDisplayName => name;

        /// <summary>When true, <see cref="SoulRealmWeaponAbilityController"/> may activate this while Soul Realm is active.</summary>
        public virtual bool AllowActivationInSoulRealm => true;

        /// <summary>When true, ability may activate in the physical realm (Soul Realm off).</summary>
        public virtual bool AllowActivationInPhysicalRealm => false;

        public abstract void Activate(in SoulWeaponAbilityContext context);

        /// <summary>
        /// Call when the ability actually commits (after early-out checks). Uses prefab if set, otherwise optional procedural burst.
        /// </summary>
        protected void PlayDefaultActivationVfx(in SoulWeaponAbilityContext context)
        {
            PlayDefaultActivationVfxAt(context, context.OriginWorld + activationVfxOffset);
        }

        /// <summary>Spawn activation VFX at a world position (e.g. raycast hit).</summary>
        protected void PlayDefaultActivationVfxAt(in SoulWeaponAbilityContext context, Vector3 worldPosition)
        {
            if (!Application.isPlaying)
                return;

            if (activationVfxPrefab != null)
            {
                Quaternion rot = Quaternion.identity;
                if (rotateVfxToAbilityForward && context.ForwardWorld.sqrMagnitude > 1e-6f)
                    rot = Quaternion.LookRotation(context.ForwardWorld.normalized, Vector3.up);
                var go = Object.Instantiate(activationVfxPrefab, worldPosition, rot);
                if (activationVfxLifetimeSeconds > 0f)
                    Object.Destroy(go, activationVfxLifetimeSeconds);
                return;
            }

            if (proceduralBurstIfNoPrefab)
                SoulAbilityProceduralBurst.Spawn(worldPosition, context.ForwardWorld, proceduralBurstColor);
        }
    }
}

