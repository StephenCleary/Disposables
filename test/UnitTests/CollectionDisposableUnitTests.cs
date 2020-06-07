using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class CollectionDisposableUnitTests
    {
        [Fact]
        public void Dispose_DisposesChild()
        {
            bool actionInvoked = false;
            var disposable = CollectionDisposable.Create(new AnonymousDisposable(() => { actionInvoked = true; }));
            disposable.Dispose();
            Assert.True(actionInvoked);
        }

        [Fact]
        public void Dispose_MultipleChildren_DisposesBothChildren()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var disposable = CollectionDisposable.Create(new AnonymousDisposable(() => { action1Invoked = true; }), new AnonymousDisposable(() => { action2Invoked = true; }));
            disposable.Dispose();
            Assert.True(action1Invoked);
            Assert.True(action2Invoked);
        }

        [Fact]
        public void Dispose_EnumerableChildren_DisposesAllChildren()
        {
            var action1Invoked = new BoolHolder();
            var action2Invoked = new BoolHolder();
            var disposable = CollectionDisposable.Create(new[] { action1Invoked, action2Invoked }.Select(bh => new AnonymousDisposable(() => { bh.Value = true; })));
            disposable.Dispose();
            Assert.True(action1Invoked.Value);
            Assert.True(action2Invoked.Value);
        }

        [Fact]
        public void Dispose_AfterAdd_DisposesBothChildren()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var disposable = CollectionDisposable.Create(new AnonymousDisposable(() => { action1Invoked = true; }));
            disposable.Add(new AnonymousDisposable(() => { action2Invoked = true; }));
            disposable.Dispose();
            Assert.True(action1Invoked);
            Assert.True(action2Invoked);
        }

        [Fact]
        public async Task Add_AfterDisposeStarts_DisposesNewChildImmediately()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();
            var disposable = CollectionDisposable.Create(new AnonymousDisposable(() =>
            {
                ready.Set();
                signal.Wait();
                action1Invoked = true;
            }));
            var task = Task.Run(() => disposable.Dispose());
            ready.Wait();
            disposable.Add(new AnonymousDisposable(() => { action2Invoked = true; }));
            Assert.False(action1Invoked);
            Assert.True(action2Invoked);
            signal.Set();
            await task;
            Assert.True(action1Invoked);
        }

        [Fact]
        public void MultipleDispose_OnlyDisposesChildOnce()
        {
            var counter = 0;
            var disposable = new CollectionDisposable(new AnonymousDisposable(() => { ++counter; }));
            disposable.Dispose();
            disposable.Dispose();
            Assert.Equal(1, counter);
        }

        private sealed class BoolHolder
        {
            public bool Value { get; set; }
        }
    }
}
