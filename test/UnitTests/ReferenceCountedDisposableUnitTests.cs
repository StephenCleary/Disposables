using System;
using System.Threading.Tasks;
using Nito.Disposables;
using System.Linq;
using System.Threading;
using Xunit;

namespace UnitTests
{
    public class ReferenceCountedDisposableUnitTests
    {
        [Fact]
        public void Create_NullDisposable_DoesNotThrow()
        {
            var disposable = ReferenceCountedDisposable.Create<IDisposable>(null);
            disposable.Dispose();
        }

        [Fact]
        public void AdvancedCreate_NullDisposable_DoesNotThrow()
        {
            var disposable = ReferenceCountedDisposable.CreateWithNewReferenceCounter<IDisposable>(null);
            disposable.Dispose();
        }

        [Fact]
        public void Target_ReturnsTarget()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            Assert.Equal(target, disposable.Target);
        }

        [Fact]
        public void Target_WhenNull_ReturnsTarget()
        {
            var disposable = ReferenceCountedDisposable.Create<IDisposable>(null);
            Assert.Null(disposable.Target);
        }

        [Fact]
        public void Target_AfterDispose_Throws()
        {
            var disposable = ReferenceCountedDisposable.Create<IDisposable>(null);
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.Target);
        }

        [Fact]
        public void Target_WhenNull_AfterDispose_Throws()
        {
            var disposable = ReferenceCountedDisposable.Create<IDisposable>(null);
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.Target);
        }

        [Fact]
        public void Dispose_DisposesTarget()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            Assert.False(target.IsDisposed);
            disposable.Dispose();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        public void MultiDispose_DisposesTargetOnce()
        {
            var targetDisposeCount = 0;
            var target = new UnsafeDisposable(() => ++targetDisposeCount);
            var disposable = ReferenceCountedDisposable.Create(target);
            Assert.Equal(0, targetDisposeCount);
            disposable.Dispose();
            Assert.Equal(1, targetDisposeCount);
            disposable.Dispose();
            Assert.Equal(1, targetDisposeCount);
        }

        [Fact]
        public void AddReference_AfterDispose_Throws()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.AddReference());
        }

        [Fact]
        public void AddReference_AfterDispose_WhenAnotherReferenceExists_Throws()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var secondDisposable = disposable.AddReference();
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.AddReference());
            Assert.False(target.IsDisposed);
        }

        [Fact]
        public void Dispose_WhenAnotherReferenceExists_DoesNotDisposeTarget_UntilOtherReferenceIsDisposed()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var secondDisposable = disposable.AddReference();
            Assert.False(target.IsDisposed);
            disposable.Dispose();
            Assert.False(target.IsDisposed);
            secondDisposable.Dispose();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        public void MultiDispose_OnlyDecrementsReferenceCountOnce()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            _ = disposable.AddReference();
            Assert.False(target.IsDisposed);
            disposable.Dispose();
            Assert.False(target.IsDisposed);
            disposable.Dispose();
            Assert.False(target.IsDisposed);
        }

        [Fact]
        public void MultiCreate_SameTarget_SharesReferenceCount()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var secondDisposable = ReferenceCountedDisposable.Create(target);
            disposable.Dispose();
            Assert.False(target.IsDisposed);
            secondDisposable.Dispose();
            Assert.True(target.IsDisposed);
        }

        [Fact]
        public void MultiTryCreate_SameTarget_AfterDisposal_ReturnsNull()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            disposable.Dispose();
            var secondDisposable = ReferenceCountedDisposable.TryCreate(target);
            Assert.Null(secondDisposable);
        }

        [Fact]
        public void MultiCreate_SameTarget_AfterDisposal_Throws()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => ReferenceCountedDisposable.Create(target));
        }

        [Fact]
        public void AddWeakReference_AfterDispose_Throws()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            disposable.Dispose();
            Assert.Throws<ObjectDisposedException>(() => disposable.AddWeakReference());
        }

        [Fact]
        public void WeakReferenceTarget_ReturnsTarget()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var weakDisposable = disposable.AddWeakReference();
            Assert.Equal(target, weakDisposable.TryGetTarget());
            GC.KeepAlive(disposable);
        }

        [Fact]
        public void WeakReference_IsNotCounted()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var weakDisposable = disposable.AddWeakReference();
            disposable.Dispose();
            Assert.Null(weakDisposable.TryGetTarget());
            Assert.Null(weakDisposable.TryAddReference());
            GC.KeepAlive(disposable);
            GC.KeepAlive(target);
        }

        [Fact]
        public void WeakReference_NotDisposed_CanIncrementCount()
        {
            var target = Disposable.Create(null);
            var disposable = ReferenceCountedDisposable.Create(target);
            var weakDisposable = disposable.AddWeakReference();
            var secondDisposable = weakDisposable.TryAddReference();
            Assert.NotNull(secondDisposable);
            disposable.Dispose();
            Assert.NotNull(weakDisposable.TryGetTarget());
            Assert.False(target.IsDisposed);
            secondDisposable.Dispose();
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
            var baseDisposable = ReferenceCountedDisposable.Create(baseTarget);
            var derivedDisposable = ReferenceCountedDisposable.Create(target);
            Assert.Equal(baseDisposable.Target, derivedDisposable.Target);
        }

        [Fact]
        public void CreateBase_AfterDerived_RefersToSameTarget()
        {
            var target = new DerivedDisposable();
            var baseTarget = target as BaseDisposable;
            var derivedDisposable = ReferenceCountedDisposable.Create(target);
            var baseDisposable = ReferenceCountedDisposable.Create(baseTarget);
            Assert.Equal(baseDisposable.Target, derivedDisposable.Target);
        }

        [Fact]
        public void GenericVariance_RefersToSameTarget()
        {
            var target = new DerivedDisposable();
            var derivedDisposable = ReferenceCountedDisposable.Create(target);
            var baseDisposable = derivedDisposable as IReferenceCountedDisposable<BaseDisposable>;
            Assert.NotNull(baseDisposable);
            Assert.Equal(baseDisposable.Target, derivedDisposable.Target);
        }

        [Fact]
        public void CastReferenceFromBaseToDerived_Fails()
        {
            var target = new DerivedDisposable();
            var baseTarget = target as BaseDisposable;
            var baseDisposable = ReferenceCountedDisposable.Create(baseTarget);
            var derivedDisposable = baseDisposable as IReferenceCountedDisposable<DerivedDisposable>;
            Assert.Null(derivedDisposable);
        }

        [Fact]
        public void CastTargetFromBaseToDerived_Succeeds()
        {
            var target = new DerivedDisposable();
            var baseTarget = target as BaseDisposable;
            var baseDisposable = ReferenceCountedDisposable.Create(baseTarget);
            var derivedTarget = baseDisposable.Target as DerivedDisposable;
            Assert.NotNull(derivedTarget);
            Assert.Equal(derivedTarget, target);
        }

        private sealed class UnsafeDisposable : IDisposable
        {
            public UnsafeDisposable(Action action) => _action = action;

            public void Dispose() => _action();

            private readonly Action _action;
        }

        private class BaseDisposable : IDisposable
        {
            public void Dispose() { }
        }

        private class DerivedDisposable : BaseDisposable
        {
        }
    }
}
