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
        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then this instance does nothing when it is disposed.</param>
        public static IReferenceCountedDisposable<T> CreateWithNewReferenceCounter<T>(T? disposable)
            where T : class, IDisposable
            => new ReferenceCountedDisposable<T>(disposable);
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

        /// <inheritdoc />
        public T Target => Context.TryGetTarget() ?? throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));

        /// <summary>
        /// Adds a (strong) reference to this reference counted disposable. If the underlying disposable has already been disposed, returns <c>null</c>.
        /// </summary>
        public IReferenceCountedDisposable<T> AddReference()
        {
            var context = Context;
            if (!context.TryIncrementCount())
                throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
            return new ReferenceCountedDisposable<T>(context);
        }

        /// <summary>
        /// Adds a weak reference to this reference counted disposable. If this <see cref="ReferenceCountedDisposable"/> has already been disposed, returns <c>null</c>.
        /// </summary>
        public IWeakReferenceCountedDisposable<T>? TryAddWeakReference() => WeakReference.TryCreate(this);

        /// <summary>
        /// Adds a weak reference to this reference counted disposable. Throws an exception if this <see cref="ReferenceCountedDisposable"/> has already been disposed.
        /// </summary>
        public IWeakReferenceCountedDisposable<T> AddWeakReference() => TryAddWeakReference() ?? AddReferenceExtensions.ThrowDisposedTargetException<IWeakReferenceCountedDisposable<T>>();

        private IReferenceCounter<T> Context
        {
            get
            {
                IReferenceCounter<T> result = null!;
                // Implementation note: IncrementCount always "succeeds" in updating the context since it always returns the same instance.
                // So, we know that IncrementCount will be called at most once. It may also be called zero times if this instance is disposed.
                if (!TryUpdateContext(x => result = (IReferenceCounter<T>)x))
                    throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
                return result;
            }
        }

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

            public T? TryGetTarget()
            {
                if (!_weakReference.TryGetTarget(out var referenceCounter))
                    return null;
                return referenceCounter.TryGetTarget();
            }
        }
    }
}
