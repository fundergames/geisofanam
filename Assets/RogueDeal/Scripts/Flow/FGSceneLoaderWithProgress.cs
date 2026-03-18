using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace Funder.GameFlow
{
    public static class FGSceneLoaderWithProgress
    {
        private static Scene _previousActiveScene;

        public static async Task LoadExclusiveWithProgress(string sceneName, LoadingScreenManager loadingScreen = null)
        {
            _previousActiveScene = SceneManager.GetActiveScene();

            var loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            
            if (loadOperation == null)
            {
                Debug.LogError($"[FGSceneLoader] Failed to load scene '{sceneName}'. " +
                    $"Make sure it's added to Build Settings (File → Build Settings → Add Open Scenes).");
                return;
            }

            loadOperation.allowSceneActivation = false;

            while (!loadOperation.isDone)
            {
                float progress = Mathf.Clamp01(loadOperation.progress / 0.9f);

                loadingScreen?.UpdateProgress(progress);

                if (loadOperation.progress >= 0.9f)
                {
                    loadingScreen?.UpdateProgress(1f);
                    loadOperation.allowSceneActivation = true;
                }

                await Task.Yield();
            }

            await Task.Yield();

            var newScene = SceneManager.GetSceneByName(sceneName);
            if (!newScene.IsValid())
            {
                Debug.LogError($"[FGSceneLoader] Scene '{sceneName}' loaded but is not valid!");
                return;
            }

            DisableEventSystemsInScene(newScene);

            SceneManager.SetActiveScene(newScene);

            if (_previousActiveScene.IsValid() && _previousActiveScene.name != sceneName)
            {
                await SceneManager.UnloadSceneAsync(_previousActiveScene);
            }

            await Task.Yield();

            EnableEventSystemsInScene(newScene);
        }

        private static void DisableEventSystemsInScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var eventSystems = rootObject.GetComponentsInChildren<EventSystem>(true);
                foreach (var eventSystem in eventSystems)
                {
                    Debug.Log($"[FGSceneLoader] Disabling EventSystem in scene '{scene.name}': {eventSystem.gameObject.name}");
                    eventSystem.enabled = false;
                }
            }
        }

        private static void EnableEventSystemsInScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var eventSystems = rootObject.GetComponentsInChildren<EventSystem>(true);
                foreach (var eventSystem in eventSystems)
                {
                    Debug.Log($"[FGSceneLoader] Enabling EventSystem in scene '{scene.name}': {eventSystem.gameObject.name}");
                    eventSystem.enabled = true;
                }
            }
        }
    }
}
