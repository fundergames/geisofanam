/*
 * Copyright (c) 2026 Funder Games
 *
 * All rights reserved.
 */

using UnityEngine;

namespace Geis.Locomotion
{
    /// <summary>
    /// ScriptableObject bundle for player locomotion tuning. Assigned on the Player prefab;
    /// values are copied into <see cref="GeisPlayerAnimationController"/> on Awake.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerLocomotionProfiles", menuName = "Funder Games/Geis/Locomotion/Player Locomotion Profile Bundle")]
    public sealed class GeisPlayerLocomotionProfileBundle : ScriptableObject
    {
        public GeisLocomotionSpeedProfile speedProfile;
        public GeisAirMovementProfile airMovementProfile;
        public GeisAttackDodgeLocomotionProfile attackDodgeProfile;
        public GeisGroundingProfile groundingProfile;
        public GeisPlayerCapsuleProfile capsuleProfile;
        public GeisStrafeInputProfile strafeInputProfile;
        public GeisLookLeanCurvesProfile lookLeanProfile;
        public GeisBowPresentationProfile bowPresentationProfile;
    }

    /// <summary>
    /// Applies locomotion profile assets (or code defaults) onto the player controller.
    /// </summary>
    public static class GeisPlayerLocomotionProfileApplier
    {
        private const string DefaultBundleResourcePath = "Movement/PlayerLocomotionProfiles";

        public static GeisPlayerLocomotionProfileBundle ResolveBundle(GeisPlayerLocomotionProfileBundle assigned) =>
            assigned != null ? assigned : Resources.Load<GeisPlayerLocomotionProfileBundle>(DefaultBundleResourcePath);

        public static void ApplyDefaults(GeisPlayerAnimationController host)
        {
            if (host == null)
                return;

            host.ApplySpeedDefaults();
            host.ApplyAirMovementDefaults();
            host.ApplyAttackDodgeDefaults();
            host.ApplyGroundingDefaults();
            host.ApplyCapsuleDefaults();
            host.ApplyStrafeInputDefaults();
            host.ApplyLookLeanDefaults();
            host.ApplyBowPresentationDefaults();
        }

        public static void Apply(GeisPlayerAnimationController host, GeisPlayerLocomotionProfileBundle bundle)
        {
            if (host == null || bundle == null)
                return;

            if (bundle.speedProfile != null)
                host.ApplySpeedProfile(bundle.speedProfile);

            if (bundle.airMovementProfile != null)
                host.ApplyAirMovementProfile(bundle.airMovementProfile);

            if (bundle.attackDodgeProfile != null)
                host.ApplyAttackDodgeProfile(bundle.attackDodgeProfile);

            if (bundle.groundingProfile != null)
                host.ApplyGroundingProfile(bundle.groundingProfile);

            if (bundle.capsuleProfile != null)
                host.ApplyCapsuleProfile(bundle.capsuleProfile);

            if (bundle.strafeInputProfile != null)
                host.ApplyStrafeInputProfile(bundle.strafeInputProfile);

            if (bundle.lookLeanProfile != null)
                host.ApplyLookLeanProfile(bundle.lookLeanProfile);

            if (bundle.bowPresentationProfile != null)
                host.ApplyBowPresentationProfile(bundle.bowPresentationProfile);
        }
    }
}
