using System.Threading.Tasks;
using Funder.Core.Services;
using Funder.Core.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;
using Funder.Core.Events;
using Funder.GameFlow.Events;

namespace Funder.GameFlow
{
    public static class FGFlowExtensions
    {
        public static async Task GoToWithLoading(FGFlow.State nextState, FGAppConfig config, string loadingMessage = null, bool showProgress = true)
        {
            if (string.IsNullOrEmpty(loadingMessage))
            {
                loadingMessage = GetLoadingMessage(nextState);
            }

            var loadingScreen = LoadingScreenManager.Instance;
            bool hasLoadingScreen = loadingScreen != null && config != null && config.ShowLoadingCanvas;

            if (hasLoadingScreen)
            {
                PublishLoadingScreenEvent("show", loadingMessage, ResolveScene(nextState, config));
                await loadingScreen.ShowAsync(loadingMessage);
                loadingScreen.UpdateProgress(0f);
            }

            float startTime = Time.realtimeSinceStartup;

            if (showProgress && hasLoadingScreen)
            {
                await LoadSceneWithProgress(nextState, config, loadingScreen, startTime);
            }
            else
            {
                await FGFlow.GoTo(nextState, config);
                if (hasLoadingScreen)
                {
                    loadingScreen.UpdateProgress(1f);
                }
            }

            if (hasLoadingScreen)
            {
                await Task.Delay(200);
                await loadingScreen.HideAsync();
                PublishLoadingScreenEvent("hide", loadingMessage, ResolveScene(nextState, config));
            }
        }

        private static async Task LoadSceneWithProgress(FGFlow.State nextState, FGAppConfig config, LoadingScreenManager loadingScreen, float startTime)
        {
            var targetScene = ResolveScene(nextState, config);
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogError($"[FGFlowExtensions] No scene configured for state {nextState}.");
                return;
            }

            var fromScene = SceneManager.GetActiveScene().name;

            await FGSceneLoaderWithProgress.LoadExclusiveWithProgress(targetScene, loadingScreen);

            float loadTime = Time.realtimeSinceStartup - startTime;

            PublishSceneTransitionEvent(fromScene, targetScene, nextState.ToString(), loadTime);

            Debug.Log($"[FLOW] Transitioned to {nextState}.");
        }

        public static async Task OnLoginCompleteWithLoading()
        {
            var config = FGConfigManager.GetConfig();
            await GoToWithLoading(FGFlow.State.Menu, config, "Loading Main Menu...");
        }

        public static async Task GoToMenuWithLoading()
        {
            var config = FGConfigManager.GetConfig();
            await GoToWithLoading(FGFlow.State.Menu, config, "Loading Main Menu...");
        }

        public static async Task GoToLoginWithLoading()
        {
            var config = FGConfigManager.GetConfig();
            await GoToWithLoading(FGFlow.State.Login, config, "Loading Login...");
        }

        /// <summary>Awaitable version for EntryController when skipping main menu.</summary>
        public static async Task GoToGameWithLoading()
        {
            var config = FGConfigManager.GetConfig();
            if (config == null)
            {
                Debug.LogError("[FGFlow] Cannot start game - FGAppConfig not found!");
                return;
            }
            await GoToWithLoading(FGFlow.State.Game, config, "Joining Lobby...");
        }

        public static async void StartGameWithLoading()
        {
            Debug.Log("[FGFlow] StartGameWithLoading called");
            var config = FGConfigManager.GetConfig();
            if (config == null)
            {
                Debug.LogError("[FGFlow] Cannot start game - FGAppConfig not found! Ensure AppConfig_RogueDeal exists at Assets/RogueDeal/Resources/Configs/AppConfig_RogueDeal.asset");
                return;
            }
            try
            {
                await GoToWithLoading(FGFlow.State.Game, config, "Joining Lobby...");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FGFlow] Failed to transition to game: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static async void FinishGameWithLoading()
        {
            var config = FGConfigManager.GetConfig();
            await GoToWithLoading(FGFlow.State.Results, config, "Loading Results...");
        }

        public static async void BackToMenuWithLoading()
        {
            var config = FGConfigManager.GetConfig();
            await GoToWithLoading(FGFlow.State.Menu, config, "Returning to Menu...");
        }

        private static string GetLoadingMessage(FGFlow.State state)
        {
            return state switch
            {
                FGFlow.State.Login => "Loading Login...",
                FGFlow.State.Menu => "Loading Main Menu...",
                FGFlow.State.Game => "Joining Lobby...",
                FGFlow.State.Results => "Loading Results...",
                _ => "Loading..."
            };
        }

        private static string ResolveScene(FGFlow.State state, FGAppConfig config)
        {
            return state switch
            {
                FGFlow.State.Login => config.LoginSceneName,
                FGFlow.State.Menu => config.MenuSceneName,
                FGFlow.State.Game => config.GameSceneName,
                FGFlow.State.Results => config.ResultsSceneName,
                _ => null
            };
        }

        private static void PublishSceneTransitionEvent(string fromScene, string toScene, string trigger, float loadTime)
        {
            try
            {
                var eventBus = GameBootstrap.ServiceLocator?.Resolve<IEventBus>();
                eventBus?.Publish(new SceneTransitionEvent
                {
                    FromScene = fromScene,
                    ToScene = toScene,
                    Trigger = trigger,
                    LoadTime = loadTime
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FGFlowExtensions] Failed to publish SceneTransitionEvent: {ex.Message}");
            }
        }

        private static void PublishLoadingScreenEvent(string action, string message, string targetScene)
        {
            try
            {
                var eventBus = GameBootstrap.ServiceLocator?.Resolve<IEventBus>();
                eventBus?.Publish(new LoadingScreenEvent
                {
                    Action = action,
                    Message = message,
                    TargetScene = targetScene
                });
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FGFlowExtensions] Failed to publish LoadingScreenEvent: {ex.Message}");
            }
        }
    }
}
