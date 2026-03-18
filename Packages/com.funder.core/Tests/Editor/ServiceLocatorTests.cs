using Funder.Core.Services;
using NUnit.Framework;

namespace Funder.Core.Tests
{
    public class ServiceLocatorTests
    {
        private interface ITestService
        {
            int Value { get; }
        }

        private sealed class TestService : ITestService
        {
            public int Value { get; }

            public TestService(int value)
            {
                Value = value;
            }
        }

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Instance.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Instance.Clear();
        }

        [Test]
        public void RegisterAndGet_ReturnsRegisteredService()
        {
            TestService service = new TestService(42);
            ServiceLocator.Instance.Register<ITestService>(service);

            ITestService resolved = ServiceLocator.Instance.Get<ITestService>();
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.Value, Is.EqualTo(42));
        }
    }
}
