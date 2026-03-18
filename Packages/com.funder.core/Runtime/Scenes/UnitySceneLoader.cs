using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Funder.Core.Scenes
{
    /// <summary>
    /// Scene loader that uses Unity SceneManager.
    /// </summary>
    public sealed class UnitySceneLoader : ISceneLoader
    {
        public async Task LoadAsync(string sceneName, bool additive = false)
        {
            var mode = additive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            var op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
                return;
            while (!op.isDone)
                await Task.Yield();
        }
    }
}
