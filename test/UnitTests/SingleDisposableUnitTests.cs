using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace UnitTests
{
    public class SingleDisposableUnitTests
    {
        [Fact]
        public void ConstructedWithNullContext_DisposeIsANoop()
        {
            var disposable = new DelegateSingleDisposable<object>(null, _ => { Assert.False(true, "Callback invoked"); });
            disposable.Dispose();
        }

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
        public async Task DisposableWaitsForDisposeToComplete()
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

            var task2 = Task.Run(() => disposable.Dispose());
            var timer = Task.Delay(500);
            Assert.Same(timer, await Task.WhenAny(task1, task2, timer));

            signal.Set();
            await task1;
            await task2;
        }

        private sealed class DelegateSingleDisposable<T> : SingleDisposable<T>
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
