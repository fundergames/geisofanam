namespace Funder.Core.Randoms
{
    public interface IRandomStream
    {
        int NextInt(int minInclusive, int maxExclusive);
        float NextFloat01();
        float NextFloat(float minInclusive, float maxInclusive);
    }
}
