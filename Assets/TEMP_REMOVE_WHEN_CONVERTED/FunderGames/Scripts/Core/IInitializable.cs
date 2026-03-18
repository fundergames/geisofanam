using System.Threading.Tasks;

namespace FunderGames.Core
{
    public interface IInitializable
    {
        Task InitializeAsync();
    }
}