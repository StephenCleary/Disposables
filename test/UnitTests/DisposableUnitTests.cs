using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class DisposableUnitTests
    {
        [Fact]
        public void Dispose_NullAction_DoesNotThrow()
        {
            var disposable = Disposable.Create(null);
            disposable.Dispose();
        }

        [Fact]
        public void Dispose_InvokesAction()
        {
            bool actionInvoked = false;
            var disposable = Disposable.Create(() => { actionInvoked = true; });
            disposable.Dispose();
            Assert.True(actionInvoked);
        }

        [Fact]
        public void Dispose_AfterAdd_InvokesBothActions()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var disposable = Disposable.Create(() => { action1Invoked = true; });
            disposable.Add(() => { action2Invoked = true; });
            disposable.Dispose();
            Assert.True(action1Invoked);
            Assert.True(action2Invoked);
        }

        [Fact]
        public void Dispose_AfterAddingNull_DoesNotThrow()
        {
            bool action1Invoked = false;
            var disposable = Disposable.Create(() => { action1Invoked = true; });
            disposable.Add(null);
            disposable.Dispose();
            Assert.True(action1Invoked);
        }

        [Fact]
        public async Task Add_AfterDisposeStarts_InvokesActionAfterDisposeCompletes()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();
            var disposable = Disposable.Create(() =>
            {
                action1Invoked = true;
                ready.Set();
                signal.Wait();
            });
            var disposeTask = Task.Run(() => disposable.Dispose());
            ready.Wait();
            var addTask = Task.Run(() => disposable.Add(() => { action2Invoked = true; }));
            Assert.False(addTask.Wait(100));
            Assert.True(action1Invoked);
            Assert.False(action2Invoked);
            signal.Set();
            await disposeTask;
            await addTask;
            Assert.True(action2Invoked);
        }

        [Fact]
        public void MultipleDispose_OnlyInvokesActionOnce()
        {
            var counter = 0;
            var disposable = Disposable.Create(() => { ++counter; });
            disposable.Dispose();
            disposable.Dispose();
            Assert.Equal(1, counter);
        }
    }
}
