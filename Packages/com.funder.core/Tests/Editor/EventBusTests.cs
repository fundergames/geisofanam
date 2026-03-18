using Funder.Core.Events;
using NUnit.Framework;

namespace Funder.Core.Tests
{
    public class EventBusTests
    {
        private readonly struct ScoreChangedEvent : IEvent
        {
            public int Score { get; }

            public ScoreChangedEvent(int score)
            {
                Score = score;
            }
        }

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.ClearAll();
        }

        [Test]
        public void Publish_InvokesSubscribers()
        {
            int observedScore = -1;
            EventBus.Subscribe<ScoreChangedEvent>(evt => observedScore = evt.Score);

            EventBus.Publish(new ScoreChangedEvent(99));

            Assert.That(observedScore, Is.EqualTo(99));
            Assert.That(EventBus.GetSubscriberCount<ScoreChangedEvent>(), Is.EqualTo(1));
        }
    }
}
