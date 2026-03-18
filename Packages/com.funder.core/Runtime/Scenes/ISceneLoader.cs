using System.Threading.Tasks;

namespace Funder.Core.Scenes
{
    public interface ISceneLoader
    {
        Task LoadAsync(string sceneName, bool additive = false);
    }
}
