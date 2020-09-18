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
        public void Dispose_NullChild_DoesNotThrow()
        {
            var disposable = CollectionDisposable.Create((IDisposable) null);
            disposable.Dispose();
        }

        [Fact]
        public void Dispose_DisposesChild()
        {
            bool actionInvoked = false;
            var disposable = CollectionDisposable.Create(new Disposable(() => { actionInvoked = true; }));
            disposable.Dispose();
            Assert.True(actionInvoked);
        }

        [Fact]
        public void Dispose_MultipleChildren_DisposesBothChildren()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var disposable = CollectionDisposable.Create(new Disposable(() => { action1Invoked = true; }), new Disposable(() => { action2Invoked = true; }));
            disposable.Dispose();
            Assert.True(action1Invoked);
            Assert.True(action2Invoked);
        }

        [Fact]
        public void Dispose_EnumerableChildren_DisposesAllChildren()
        {
            var action1Invoked = new BoolHolder();
            var action2Invoked = new BoolHolder();
            var disposable = CollectionDisposable.Create(new[] { action1Invoked, action2Invoked }.Select(bh => new Disposable(() => { bh.Value = true; })));
            disposable.Dispose();
            Assert.True(action1Invoked.Value);
            Assert.True(action2Invoked.Value);
        }

        [Fact]
        public void Dispose_AfterAdd_DisposesBothChildren()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var disposable = CollectionDisposable.Create(new Disposable(() => { action1Invoked = true; }));
            disposable.Add(new Disposable(() => { action2Invoked = true; }));
            disposable.Dispose();
            Assert.True(action1Invoked);
            Assert.True(action2Invoked);
        }
        
        [Fact]
        public void Dispose_AfterAddingNullChild_DoesNotThrow()
        {
            bool action1Invoked = false;
            var disposable = CollectionDisposable.Create(new Disposable(() => { action1Invoked = true; }));
            disposable.Add(null);
            disposable.Dispose();
            Assert.True(action1Invoked);
        }

        [Fact]
        public async Task Add_AfterDisposeStarts_DisposesNewChildAfterDisposalCompletes()
        {
            bool action1Invoked = false;
            bool action2Invoked = false;
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();
            var disposable = CollectionDisposable.Create(new Disposable(() =>
            {
                action1Invoked = true;
                ready.Set();
                signal.Wait();
            }));
            var disposeTask = Task.Run(() => disposable.Dispose());
            ready.Wait();
            var addTask = Task.Run(() => disposable.Add(new Disposable(() => { action2Invoked = true; })));
            Assert.False(addTask.Wait(100));
            Assert.True(action1Invoked);
            Assert.False(action2Invoked);
            signal.Set();
            await disposeTask;
            await addTask;
            Assert.True(action2Invoked);
        }

        [Fact]
        public void MultipleDispose_OnlyDisposesChildOnce()
        {
            var counter = 0;
            var disposable = new CollectionDisposable(new Disposable(() => { ++counter; }));
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
