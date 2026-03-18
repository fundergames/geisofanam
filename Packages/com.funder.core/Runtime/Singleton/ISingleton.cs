namespace Funder.Core.Singleton
{
    public interface ISingleton
    {
        void OnSingletonAwake();
        void OnSingletonDestroy();
    }
}
