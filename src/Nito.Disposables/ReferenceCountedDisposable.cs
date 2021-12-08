using System;
using System.Threading;
using Nito.Disposables.Internals;

namespace Nito.Disposables
{
    /// <summary>
    /// An instance that refers to an underlying target and can add reference counts to the counter for that target.
    /// </summary>
    public interface IReferenceCounterReference<out T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Adds a reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        IReferenceCountedDisposable<T>? TryAddReference();
    }

    /// <summary>
    /// An instance that represents a reference count.
    /// </summary>
    public interface IReferenceCountedDisposable<out T> : IDisposable, IReferenceCounterReference<T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Adds a weak reference to this reference counted disposable. If this instance has already been disposed, returns <c>null</c>.
        /// </summary>
        IWeakReferenceCountedDisposable<T>? TryAddWeakReference();
    }

    /// <summary>
    /// An instance that represents an uncounted weak reference.
    /// </summary>
    public interface IWeakReferenceCountedDisposable<out T> : IReferenceCounterReference<T>
        where T : class, IDisposable
    {
    }

    /// <summary>
    /// Creation methods for reference counted disposables.
    /// </summary>
    public static class ReferenceCountedDisposable
    {
        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public static ReferenceCountedDisposable<T> Create<T>(T? disposable)
            where T : class, IDisposable
            => new(disposable);
    }

    /// <summary>
    /// An instance that represents a reference count.
    /// </summary>
    public sealed class ReferenceCountedDisposable<T> : SingleDisposable<object>, IReferenceCountedDisposable<T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public ReferenceCountedDisposable(T? disposable)
            : this(new ReferenceCounter<T>(disposable))
        {
        }

        private ReferenceCountedDisposable(IReferenceCounter<T> referenceCount)
            : base(referenceCount)
        {
        }

        /// <inheritdoc />
        protected override void Dispose(object context)
        {
            var referenceCount = (IReferenceCounter<T>)context;
            referenceCount.TryDecrementCount()?.Dispose();
        }

        /// <summary>
        /// Adds a (strong) reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public IReferenceCountedDisposable<T>? TryAddReference()
        {
            IReferenceCounter<T> referenceCount = null!;
            // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
            // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
            if (!TryUpdateContext(x => referenceCount = ((IReferenceCounter<T>)x).TryIncrementCount() ? (IReferenceCounter<T>)x : null!))
                return null;
            return new ReferenceCountedDisposable<T>(referenceCount);
        }

        /// <summary>
        /// Adds a weak reference to this reference counted disposable. If this <see cref="ReferenceCountedDisposable"/> has already been disposed, returns <c>null</c>.
        /// </summary>
        public IWeakReferenceCountedDisposable<T>? TryAddWeakReference() => WeakReference.TryCreate(this);

        /// <summary>
        /// Adds a weak reference to this reference counted disposable. Throws an exception if this <see cref="ReferenceCountedDisposable"/> has already been disposed.
        /// </summary>
        public IWeakReferenceCountedDisposable<T> AddWeakReference() => TryAddWeakReference() ?? AddReferenceExtensions.ThrowDisposedTargetException<IWeakReferenceCountedDisposable<T>>();

        private sealed class WeakReference : IWeakReferenceCountedDisposable<T>
        {
            private readonly WeakReference<IReferenceCounter<T>> _weakReference;

            private WeakReference(IReferenceCounter<T> referenceCount)
            {
                _weakReference = new(referenceCount);
            }

            public static WeakReference? TryCreate(ReferenceCountedDisposable<T> referenceCountedDisposable)
            {
                _ = referenceCountedDisposable ?? throw new ArgumentNullException(nameof(referenceCountedDisposable));
                IReferenceCounter<T> referenceCount = null!;
                // Implementation note: TryUpdateContext always "succeeds" in updating the context since the lambda always returns the same instance.
                // The only way this isn't the case is if the reference counted disposable has been disposed.
                if (!referenceCountedDisposable.TryUpdateContext(x => referenceCount = (IReferenceCounter<T>)x))
                    return null;
                return new(referenceCount);
            }

            public IReferenceCountedDisposable<T>? TryAddReference()
            {
                if (!_weakReference.TryGetTarget(out var referenceCount))
                    return null;
                if (!referenceCount.TryIncrementCount())
                    return null;
                return new ReferenceCountedDisposable<T>(referenceCount);
            }
        }
    }
}
