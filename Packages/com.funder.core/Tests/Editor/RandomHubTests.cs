using Funder.Core.Randoms;
using NUnit.Framework;

namespace Funder.Core.Tests
{
    public class RandomHubTests
    {
        [Test]
        public void SameSeedAndStream_ProducesSameSequence()
        {
            RandomHub hubA = new RandomHub(12345);
            RandomHub hubB = new RandomHub(12345);

            IRandomStream streamA = hubA.GetStream("puzzle_generation");
            IRandomStream streamB = hubB.GetStream("puzzle_generation");

            for (int i = 0; i < 10; i++)
            {
                Assert.That(streamA.NextInt(0, 1000), Is.EqualTo(streamB.NextInt(0, 1000)));
            }
        }

        [Test]
        public void DifferentStreams_ProduceDifferentSequences()
        {
            RandomHub hub = new RandomHub(777);
            IRandomStream streamA = hub.GetStream("a");
            IRandomStream streamB = hub.GetStream("b");

            bool anyDifference = false;
            for (int i = 0; i < 10; i++)
            {
                if (streamA.NextInt(0, 1000) != streamB.NextInt(0, 1000))
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.That(anyDifference, Is.True);
        }
    }
}
