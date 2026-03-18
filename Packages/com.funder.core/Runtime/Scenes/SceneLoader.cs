using System.Threading.Tasks;
using Funder.Core.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Funder.Core.Scenes
{
    /// <summary>
    /// Scene loader that uses Unity SceneManager and optionally logs via ILoggingService.
    /// </summary>
    public sealed class SceneLoader : ISceneLoader
    {
        private readonly ILoggingService _logger;

        public SceneLoader(ILoggingService logger = null)
        {
            _logger = logger;
        }

        public async Task LoadAsync(string sceneName, bool additive = false)
        {
            _logger?.Info("SceneLoader", $"Loading scene: {sceneName} (additive: {additive})");
            var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                _logger?.Error("SceneLoader", $"Failed to load scene: {sceneName}");
                return;
            }
            while (!op.isDone)
                await Task.Yield();
        }
    }
}
