using System.Threading.Tasks;
using UnityEngine;
using Funder.Core.Services;
using Funder.Core.Flow;

namespace Funder.GameFlow
{
    public class EntryController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Optional - Bootstrap config for this product")]
        private ProductBootstrap productBootstrap;

        [SerializeField]
        [Tooltip("Optional - Direct config reference (overrides ProductBootstrap)")]
        private FGAppConfig appConfig;

        [SerializeField]
        private float initializationDelay = 0.5f;

        private async void Start()
        {
            Debug.Log("[Entry] Starting game initialization...");

            await Task.Delay((int)(initializationDelay * 1000));

            if (productBootstrap != null)
            {
                productBootstrap.Initialize();
            }

            if (appConfig == null)
            {
                appConfig = FGConfigManager.GetConfig();
            }

            if (appConfig == null)
            {
                Debug.LogError("[Entry] FGAppConfig not found! Cannot proceed.");
                return;
            }

            // Go directly to third-person combat scene
            Debug.Log("[Entry] Loading third-person combat...");
            await FGFlowExtensions.GoToGameWithLoading();
        }
    }
}
