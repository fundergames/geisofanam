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

            await ShowSplash();

            if (ShouldSkipLogin())
            {
                if (Application.isEditor && appConfig.SkipMainMenuInEditor)
                {
                    Debug.Log("[Entry] Skipping main menu - going directly to game (SkipMainMenuInEditor)");
                    await FGFlowExtensions.GoToGameWithLoading();
                }
                else
                {
                    Debug.Log("[Entry] Skipping login - going directly to main menu");
                    await FGFlowExtensions.GoToMenuWithLoading();
                }
            }
            else
            {
                Debug.Log("[Entry] Going to login screen");
                await FGFlowExtensions.GoToLoginWithLoading();
            }
        }

        private async Task ShowSplash()
        {
            Debug.Log("[Entry] Loading splash screen...");
            await FGSceneLoader.LoadExclusive("Splash");
            await Task.Delay(2000);
        }

        private bool ShouldSkipLogin()
        {
            if (Application.isEditor && appConfig.SkipLoginInEditor)
            {
                return true;
            }

            if (!Application.isEditor && appConfig.SkipLoginInBuilds)
            {
                return true;
            }

            return false;
        }
    }
}
