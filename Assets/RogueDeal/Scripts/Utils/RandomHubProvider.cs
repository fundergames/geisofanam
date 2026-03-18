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
