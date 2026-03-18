using Funder.Core.FSM;
using NUnit.Framework;

namespace Funder.Core.Tests
{
    public class StateMachineTests
    {
        private sealed class TestStateA : StateNode
        {
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }

            public override void OnEnter()
            {
                EnterCount++;
            }

            public override void OnExit()
            {
                ExitCount++;
            }
        }

        private sealed class TestStateB : StateNode
        {
            public int EnterCount { get; private set; }

            public override void OnEnter()
            {
                EnterCount++;
            }
        }

        [Test]
        public void TryGo_AllowsConfiguredTransition()
        {
            StateMachine machine = new StateMachine();
            TestStateA a = new TestStateA();
            TestStateB b = new TestStateB();

            machine.AddState(a);
            machine.AddState(b);
            machine.AddTransition(typeof(TestStateA), typeof(TestStateB));

            Assert.That(machine.TryGo<TestStateA>(), Is.True);
            Assert.That(machine.TryGo<TestStateB>(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(b));
            Assert.That(a.ExitCount, Is.EqualTo(1));
            Assert.That(b.EnterCount, Is.EqualTo(1));
        }

        [Test]
        public void TryGo_BlocksUnconfiguredTransition()
        {
            StateMachine machine = new StateMachine();
            machine.AddState(new TestStateA());
            machine.AddState(new TestStateB());

            Assert.That(machine.TryGo<TestStateA>(), Is.True);
            Assert.That(machine.TryGo<TestStateB>(), Is.False);
            Assert.That(machine.Current, Is.TypeOf<TestStateA>());
        }
    }
}
