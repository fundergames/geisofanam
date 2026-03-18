using System;
using System.Collections.Generic;

namespace Funder.Core.Randoms
{
    public class RandomHub : IRandomHub
    {
        private readonly Dictionary<string, IRandomStream> _streams = new Dictionary<string, IRandomStream>();

        public uint Seed { get; private set; }

        public RandomHub(uint seed)
        {
            Seed = seed;
        }

        public IRandomStream GetStream(string name)
        {
            string streamName = string.IsNullOrWhiteSpace(name) ? "default" : name;

            if (_streams.TryGetValue(streamName, out IRandomStream stream))
            {
                return stream;
            }

            int streamSeed = MixSeed(Seed, streamName);
            stream = new RandomStream(streamSeed);
            _streams[streamName] = stream;
            return stream;
        }

        public void Reseed(uint seed)
        {
            Seed = seed;
            _streams.Clear();
        }

        private static int MixSeed(uint rootSeed, string streamName)
        {
            unchecked
            {
                // FNV-1a 32-bit hash mixed with root seed for deterministic stream seeds.
                uint hash = 2166136261;
                for (int i = 0; i < streamName.Length; i++)
                {
                    hash ^= streamName[i];
                    hash *= 16777619;
                }

                uint mixed = hash ^ rootSeed;
                if (mixed > int.MaxValue)
                {
                    return (int)(mixed - int.MaxValue);
                }

                return (int)mixed;
            }
        }

        private sealed class RandomStream : IRandomStream
        {
            private readonly Random _random;

            public RandomStream(int seed)
            {
                _random = new Random(seed);
            }

            public int NextInt(int minInclusive, int maxExclusive)
            {
                return _random.Next(minInclusive, maxExclusive);
            }

            public float NextFloat01()
            {
                return (float)_random.NextDouble();
            }

            public float NextFloat(float minInclusive, float maxInclusive)
            {
                if (maxInclusive < minInclusive)
                {
                    (minInclusive, maxInclusive) = (maxInclusive, minInclusive);
                }

                float range = maxInclusive - minInclusive;
                return minInclusive + NextFloat01() * range;
            }
        }
    }
}
