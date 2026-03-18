namespace Funder.Core.Randoms
{
    public interface IRandomHub
    {
        uint Seed { get; }
        IRandomStream GetStream(string name);
        void Reseed(uint seed);
    }
}
