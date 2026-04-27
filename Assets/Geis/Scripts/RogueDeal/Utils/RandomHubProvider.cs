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

using Funder.Core.Randoms;
using Funder.Core.Services;
using UnityEngine;

namespace RogueDeal.Utils
{
    public static class RandomHubProvider
    {
        public static IRandomHub Get()
        {
            if (GameBootstrap.ServiceLocator.TryResolve<IRandomHub>(out var randomHub))
            {
                return randomHub;
            }

            Debug.LogError("[RandomHubProvider] IRandomHub service not found in ServiceLocator. " +
                "Ensure RandomHubService is registered in BootstrapConfig.");
            return null;
        }

        public static bool TryGet(out IRandomHub randomHub)
        {
            return GameBootstrap.ServiceLocator.TryResolve<IRandomHub>(out randomHub);
        }
    }
}
