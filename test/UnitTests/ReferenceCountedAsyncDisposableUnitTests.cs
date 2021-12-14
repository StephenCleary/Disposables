using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class ReferenceCountedAsyncDisposableUnitTests
    {
        [Fact]
        public async Task Create_NullDisposable_DoesNotThrow()
        {
            var disposable = ReferenceCountedAsyncDisposable.Create<IAsyncDisposable>(null);
            await disposable.DisposeAsync();
        }

        [Fact]
        public async Task AdvancedCreate_NullDisposable_DoesNotThrow()
        {
            var disposable = ReferenceCountedAsyncDisposable.CreateWithNewReferenceCounter<IAsyncDisposable>(null);
            await disposable.DisposeAsync();
        }

        [Fact]
        public void Target_ReturnsTarget()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            Assert.Equal(target, disposable.Target);
        }

        [Fact]
        public void Target_WhenNull_ReturnsTarget()
        {
            var disposable = ReferenceCountedAsyncDisposable.Create<IAsyncDisposable>(null);
            Assert.Null(disposable.Target);
        }

        [Fact]
        public async Task Target_AfterDispose_Throws()
        {
            var disposable = ReferenceCountedAsyncDisposable.Create<IAsyncDisposable>(null);
            await disposable.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => disposable.Target);
        }

        [Fact]
        public async Task Target_WhenNull_AfterDispose_Throws()
        {
            var disposable = ReferenceCountedAsyncDisposable.Create<IAsyncDisposable>(null);
            await disposable.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => disposable.Target);
        }

        [Fact]
        public async Task Dispose_DisposesTarget()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            Assert.False(target.IsDisposed);
            await disposable.DisposeAsync();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        public async Task MultiDispose_DisposesTargetOnceAsync()
        {
            var targetDisposeCount = 0;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            var target = new UnsafeDisposable(async () => ++targetDisposeCount);
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            Assert.Equal(0, targetDisposeCount);
            await disposable.DisposeAsync();
            Assert.Equal(1, targetDisposeCount);
            await disposable.DisposeAsync();
            Assert.Equal(1, targetDisposeCount);
        }

        [Fact]
        public async Task AddReference_AfterDispose_ThrowsAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            await disposable.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => disposable.AddReference());
        }

        [Fact]
        public async Task AddReference_AfterDispose_WhenAnotherReferenceExists_ThrowsAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            var secondDisposable = disposable.AddReference();
            await disposable.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => disposable.AddReference());
            Assert.False(target.IsDisposed);
        }

        [Fact]
        public async Task Dispose_WhenAnotherReferenceExists_DoesNotDisposeTarget_UntilOtherReferenceIsDisposedAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            var secondDisposable = disposable.AddReference();
            Assert.False(target.IsDisposed);
            await disposable.DisposeAsync();
            Assert.False(target.IsDisposed);
            await secondDisposable.DisposeAsync();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        public async Task MultiDispose_OnlyDecrementsReferenceCountOnceAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            _ = disposable.AddReference();
            Assert.False(target.IsDisposed);
            await disposable.DisposeAsync();
            Assert.False(target.IsDisposed);
            await disposable.DisposeAsync();
            Assert.False(target.IsDisposed);
        }

        [Fact]
        public async Task MultiCreate_SameTarget_SharesReferenceCountAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            var secondDisposable = ReferenceCountedAsyncDisposable.Create(target);
            await disposable.DisposeAsync();
            Assert.False(target.IsDisposed);
            await secondDisposable.DisposeAsync();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        public async Task MultiTryCreate_SameTarget_AfterDisposal_ReturnsNullAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            await disposable.DisposeAsync();
            var secondDisposable = ReferenceCountedAsyncDisposable.TryCreate(target);
            Assert.Null(secondDisposable);
        }

        [Fact]
        public async Task MultiCreate_SameTarget_AfterDisposal_ThrowsAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            await disposable.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => ReferenceCountedAsyncDisposable.Create(target));
        }

        [Fact]
        public async Task AddWeakReference_AfterDispose_ThrowsAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            await disposable.DisposeAsync();
            Assert.Throws<ObjectDisposedException>(() => disposable.AddWeakReference());
        }

        [Fact]
        public void WeakReferenceTarget_ReturnsTarget()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            var weakDisposable = disposable.AddWeakReference();
            Assert.Equal(target, weakDisposable.TryGetTarget());
            GC.KeepAlive(disposable);
        }

        [Fact]
        public async Task WeakReference_IsNotCountedAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            var weakDisposable = disposable.AddWeakReference();
            await disposable.DisposeAsync();
            Assert.Null(weakDisposable.TryGetTarget());
            Assert.Null(weakDisposable.TryAddReference());
            GC.KeepAlive(disposable);
            GC.KeepAlive(target);
        }

        [Fact]
        public async Task WeakReference_NotDisposed_CanIncrementCountAsync()
        {
            var target = AsyncDisposable.Create(null);
            var disposable = ReferenceCountedAsyncDisposable.Create(target);
            var weakDisposable = disposable.AddWeakReference();
            var secondDisposable = weakDisposable.TryAddReference();
            Assert.NotNull(secondDisposable);
            await disposable.DisposeAsync();
            Assert.NotNull(weakDisposable.TryGetTarget());
            Assert.False(target.IsDisposed);
            await secondDisposable.DisposeAsync();
            Assert.Null(weakDisposable.TryGetTarget());
            Assert.Null(weakDisposable.TryAddReference());
            GC.KeepAlive(secondDisposable);
            GC.KeepAlive(disposable);
            GC.KeepAlive(target);
        }

        [Fact]
        public void CreateDerived_AfterBase_RefersToSameTarget()
        {
            var target = new DerivedDisposable();
            var baseTarget = target as BaseDisposable;
            var baseDisposable = ReferenceCountedAsyncDisposable.Create(baseTarget);
            var derivedDisposable = ReferenceCountedAsyncDisposable.Create(target);
            Assert.Equal(baseDisposable.Target, derivedDisposable.Target);
        }

        [Fact]
        public void CreateBase_AfterDerived_RefersToSameTarget()
        {
            var target = new DerivedDisposable();
            var baseTarget = target as BaseDisposable;
            var derivedDisposable = ReferenceCountedAsyncDisposable.Create(target);
            var baseDisposable = ReferenceCountedAsyncDisposable.Create(baseTarget);
            Assert.Equal(baseDisposable.Target, derivedDisposable.Target);
        }

        [Fact]
        public void GenericVariance_RefersToSameTarget()
        {
            var target = new DerivedDisposable();
            var derivedDisposable = ReferenceCountedAsyncDisposable.Create(target);
            var baseDisposable = derivedDisposable as IReferenceCountedAsyncDisposable<BaseDisposable>;
            Assert.NotNull(baseDisposable);
            Assert.Equal(baseDisposable.Target, derivedDisposable.Target);
        }

        [Fact]
        public void CastReferenceFromBaseToDerived_Fails()
        {
            var target = new DerivedDisposable();
            var baseTarget = target as BaseDisposable;
            var baseDisposable = ReferenceCountedAsyncDisposable.Create(baseTarget);
            var derivedDisposable = baseDisposable as IReferenceCountedAsyncDisposable<DerivedDisposable>;
            Assert.Null(derivedDisposable);
        }

        [Fact]
        public void CastTargetFromBaseToDerived_Succeeds()
        {
            var target = new DerivedDisposable();
            var baseTarget = target as BaseDisposable;
            var baseDisposable = ReferenceCountedAsyncDisposable.Create(baseTarget);
            var derivedTarget = baseDisposable.Target as DerivedDisposable;
            Assert.NotNull(derivedTarget);
            Assert.Equal(derivedTarget, target);
        }

        [Fact]
        public void Create_FromBothSynchronousAndAysnchronous_ReferencesSameTarget()
        {
            var target = new UnsafeSyncAsyncDisposable(() => { });
            var syncDisposable = ReferenceCountedDisposable.Create(target);
            var asyncDisposable = ReferenceCountedAsyncDisposable.Create(target);
            Assert.Equal(syncDisposable.Target, asyncDisposable.Target);
        }

        [Fact]
        public async Task BothSyncAndAysnc_SyncDisposedFirst_OnlyDisposesWhenBothAreDisposed()
        {
            var disposeCount = 0;
            var target = new UnsafeSyncAsyncDisposable(() => ++disposeCount);
            var syncDisposable = ReferenceCountedDisposable.Create(target);
            var asyncDisposable = ReferenceCountedAsyncDisposable.Create(target);
            syncDisposable.Dispose();
            Assert.Equal(0, disposeCount);
            await asyncDisposable.DisposeAsync();
            Assert.Equal(1, disposeCount);
        }

        [Fact]
        public async Task BothSyncAndAysnc_AsyncDisposedFirst_OnlyDisposesWhenBothAreDisposed()
        {
            var disposeCount = 0;
            var target = new UnsafeSyncAsyncDisposable(() => ++disposeCount);
            var syncDisposable = ReferenceCountedDisposable.Create(target);
            var asyncDisposable = ReferenceCountedAsyncDisposable.Create(target);
            await asyncDisposable.DisposeAsync();
            Assert.Equal(0, disposeCount);
            syncDisposable.Dispose();
            Assert.Equal(1, disposeCount);
        }

        private sealed class UnsafeDisposable : IAsyncDisposable
        {
            public UnsafeDisposable(Func<Task> action) => _action = action;

            public async ValueTask DisposeAsync() => await _action();

            private readonly Func<Task> _action;
        }

        private class BaseDisposable : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => new();
        }

        private class DerivedDisposable : BaseDisposable
        {
        }

        private sealed class UnsafeSyncAsyncDisposable: IDisposable, IAsyncDisposable
        {
            private Action _action;

            public UnsafeSyncAsyncDisposable(Action action) => _action = action;
            public void Dispose() => _action();
            public ValueTask DisposeAsync()
            {
                _action();
                return new();
            }
        }
    }
}
