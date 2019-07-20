using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace UnitTests
{
    public class SingleNonblockingDisposableUnitTests
    {
        [Fact]
        public void ConstructedWithContext_DisposeReceivesThatContext()
        {
            var providedContext = new object();
            object seenContext = null;
            var disposable = new DelegateSingleDisposable<object>(providedContext, context => { seenContext = context; });
            disposable.Dispose();
            Assert.Same(providedContext, seenContext);
        }

        [Fact]
        public void DisposeOnlyCalledOnce()
        {
            var counter = 0;
            var disposable = new DelegateSingleDisposable<object>(new object(), _ => { ++counter; });
            disposable.Dispose();
            disposable.Dispose();
            Assert.Equal(1, counter);
        }

        [Fact]
        public async Task DisposeIsNonblocking()
        {
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();
            var disposable = new DelegateSingleDisposable<object>(new object(), _ =>
            {
                ready.Set();
                signal.Wait();
            });

            var task1 = Task.Run(() => disposable.Dispose());
            ready.Wait();

            await Task.Run(() => disposable.Dispose());

            signal.Set();
            await task1;
        }

        [Fact]
        public async Task LifetimeProperties_HaveAppropriateValues()
        {
            var ready = new ManualResetEventSlim();
            var signal = new ManualResetEventSlim();
            var disposable = new DelegateSingleDisposable<object>(new object(), _ =>
            {
                ready.Set();
                signal.Wait();
            });

            Assert.False(disposable.IsDisposed);

            var task1 = Task.Run(() => disposable.Dispose());
            ready.Wait();

            // Note: IsDisposed is true once disposal starts.
            Assert.True(disposable.IsDisposed);

            signal.Set();
            await task1;

            Assert.True(disposable.IsDisposed);
        }

        private sealed class DelegateSingleDisposable<T> : SingleNonblockingDisposable<T>
            where T : class
        {
            private readonly Action<T> _callback;

            public DelegateSingleDisposable(T context, Action<T> callback)
                : base(context)
            {
                _callback = callback;
            }

            protected override void Dispose(T context)
            {
                _callback(context);
            }
        }
    }
}
