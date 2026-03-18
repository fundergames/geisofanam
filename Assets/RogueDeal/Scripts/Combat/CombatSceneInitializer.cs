using UnityEngine;
using Funder.Core.Services;
using Funder.Core.Events;

namespace RogueDeal.Combat
{
    [DefaultExecutionOrder(-9000)]
    public class CombatSceneInitializer : MonoBehaviour
    {
        private void Awake()
        {
            if (GameBootstrap.ServiceLocator == null)
            {
                Debug.LogError("[CombatSceneInit] GameBootstrap not found! Combat scene must be loaded from Entry scene.");
                return;
            }

            IEventBus eventBus = null;
            try
            {
                eventBus = GameBootstrap.ServiceLocator.Resolve<IEventBus>();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CombatSceneInit] Failed to resolve IEventBus: {e.Message}");
            }

            if (eventBus != null)
            {
                Debug.Log("[CombatSceneInit] ✅ IEventBus service is registered and ready");
            }
            else
            {
                Debug.LogError("[CombatSceneInit] ❌ IEventBus service is NOT registered!");
                Debug.LogError("[CombatSceneInit] Make sure you start from the Entry scene, not directly from Combat scene!");
            }
        }
    }
}
