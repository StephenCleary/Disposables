using System;
using System.Threading;
using Nito.Disposables.Internals;

namespace Nito.Disposables
{
    /// <summary>
    /// An instance that represents a reference count.
    /// </summary>
    public interface IReferenceCountedDisposable<out T> : IDisposable
        where T : class, IDisposable
    {
        /// <summary>
        /// Adds a weak reference to this reference counted disposable. Throws <see cref="ObjectDisposedException"/> if this instance is disposed.
        /// </summary>
        IWeakReferenceCountedDisposable<T> AddWeakReference();

        /// <summary>
        /// Returns a new reference to this reference counted disposable, incrementing the reference counter. Throws <see cref="ObjectDisposedException"/> if this instance is disposed.
        /// </summary>
        IReferenceCountedDisposable<T> AddReference();

        /// <summary>
        /// Gets the target object. Throws <see cref="ObjectDisposedException"/> if this instance is disposed.
        /// </summary>
        T Target { get; }
    }

    /// <summary>
    /// An instance that represents an uncounted weak reference.
    /// </summary>
    public interface IWeakReferenceCountedDisposable<out T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Adds a reference to this reference counted disposable. If the underlying disposable has already been disposed or garbage collected, returns <c>null</c>.
        /// </summary>
        IReferenceCountedDisposable<T>? TryAddReference();

        /// <summary>
        /// Attempts to get the target object. If the underlying disposable has already been disposed or garbage collected, returns <c>null</c>.
        /// </summary>
        T? TryGetTarget();
    }

    /// <summary>
    /// Creation methods for reference counted disposables.
    /// </summary>
    public static class ReferenceCountedDisposable
    {
        // TODO: Create<T>(T? disposable)

        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public static IReferenceCountedDisposable<T> CreateWithNewReferenceCounter<T>(T? disposable)
            where T : class, IDisposable
            => new ReferenceCountedDisposable<T>(new ReferenceCounter<T>(disposable));
    }

    internal sealed class ReferenceCountedDisposable<T> : SingleDisposable<IReferenceCounter<T>>, IReferenceCountedDisposable<T>
        where T : class, IDisposable
    {
        /// <summary>
        /// Initializes a reference counted disposable that refers to the specified reference count. The specified reference count must have already been incremented for this instance.
        /// </summary>
        public ReferenceCountedDisposable(IReferenceCounter<T> referenceCounter)
            : base(referenceCounter)
        {
        }

        protected override void Dispose(IReferenceCounter<T> referenceCounter)
        {
            referenceCounter.TryDecrementCount()?.Dispose();
        }

        T IReferenceCountedDisposable<T>.Target => ReferenceCounter.TryGetTarget() ?? throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));

        IReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddReference()
        {
            var referenceCounter = ReferenceCounter;
            if (!referenceCounter.TryIncrementCount())
                throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
            return new ReferenceCountedDisposable<T>(referenceCounter);
        }

        IWeakReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddWeakReference() => new WeakReferenceCountedDisposable<T>(ReferenceCounter);

        private IReferenceCounter<T> ReferenceCounter
        {
            get
            {
                IReferenceCounter<T> referenceCounter = null!;
                // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
                // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
                if (!TryUpdateContext(x => referenceCounter = x))
                    throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
                return referenceCounter;
            }
        }
    }

    internal sealed class WeakReferenceCountedDisposable<T> : IWeakReferenceCountedDisposable<T>
        where T : class, IDisposable
    {
        private readonly WeakReference<IReferenceCounter<T>> _weakReference;

        public WeakReferenceCountedDisposable(IReferenceCounter<T> referenceCounter)
        {
            _weakReference = new(referenceCounter);
        }

        IReferenceCountedDisposable<T>? IWeakReferenceCountedDisposable<T>.TryAddReference()
        {
            if (!_weakReference.TryGetTarget(out var referenceCounter))
                return null;
            if (!referenceCounter.TryIncrementCount())
                return null;
            return new ReferenceCountedDisposable<T>(referenceCounter);
        }

        T? IWeakReferenceCountedDisposable<T>.TryGetTarget()
        {
            if (!_weakReference.TryGetTarget(out var referenceCounter))
                return null;
            return referenceCounter.TryGetTarget();
        }
    }
}
