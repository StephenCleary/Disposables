using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class AnonymousDisposableUnitTests
    {
        [Fact]
        public void Dispose_InvokesAction()
        {
            bool actionInvoked = false;
            var disposable = new AnonymousDisposable(() => { actionInvoked = true; });
            disposable.Dispose();
            Assert.True(actionInvoked);
        }

        [Fact]
        public void Dispose_AfterAdd_InvokesBothActions()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var disposable = new AnonymousDisposable(() => { action1Invoked = true; });
            disposable.Add(() => { action2Invoked = true; });
            disposable.Dispose();
            Assert.True(action1Invoked);
            Assert.True(action2Invoked);
        }

        [Fact]
        public async Task Add_AfterDisposeStarts_InvokesActionImmediately()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();
            var disposable = new AnonymousDisposable(() =>
            {
                ready.Set();
                signal.Wait();
                action1Invoked = true;
            });
            var task = Task.Run(() => disposable.Dispose());
            ready.Wait();
            disposable.Add(() => { action2Invoked = true; });
            Assert.False(action1Invoked);
            Assert.True(action2Invoked);
            signal.Set();
            await task;
            Assert.True(action1Invoked);
        }

        [Fact]
        public void MultipleDispose_OnlyInvokesActionOnce()
        {
            var counter = 0;
            var disposable = new AnonymousDisposable(() => { ++counter; });
            disposable.Dispose();
            disposable.Dispose();
            Assert.Equal(1, counter);
        }
    }
}
