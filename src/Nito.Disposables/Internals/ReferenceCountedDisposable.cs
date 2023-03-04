using System;

namespace Nito.Disposables.Internals;

/// <summary>
/// An instance that represents a reference count.
/// </summary>
public sealed class ReferenceCountedDisposable<T> : SingleDisposable<IReferenceCounter>, IReferenceCountedDisposable<T>
    where T : class, IDisposable
{
    /// <summary>
    /// Initializes a reference counted disposable that refers to the specified reference count. The specified reference count must have already been incremented for this instance.
    /// </summary>
    public ReferenceCountedDisposable(IReferenceCounter referenceCounter)
        : base(referenceCounter)
    {
        _ = referenceCounter ?? throw new ArgumentNullException(nameof(referenceCounter));

        // Ensure we can cast from the stored IDisposable to T.
        _ = ((IReferenceCountedDisposable<T>) this).Target;
    }

    /// <inheritdoc/>
    protected override void Dispose(IReferenceCounter referenceCounter) => (referenceCounter.TryDecrementCount() as IDisposable)?.Dispose();

    T? IReferenceCountedDisposable<T>.Target => (T?) ReferenceCounter.TryGetTarget();

    IReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddReference()
    {
        var referenceCounter = ReferenceCounter;
        if (!referenceCounter.TryIncrementCount())
            throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>)); // cannot actually happen
        return new ReferenceCountedDisposable<T>(referenceCounter);
    }

    IWeakReferenceCountedDisposable<T> IReferenceCountedDisposable<T>.AddWeakReference() => new WeakReferenceCountedDisposable<T>(ReferenceCounter);

    private IReferenceCounter ReferenceCounter
    {
        get
        {
            IReferenceCounter referenceCounter = null!;
            // Implementation note: this always "succeeds" in updating the context since it always returns the same instance.
            // So, we know that this will be called at most once. It may also be called zero times if this instance is disposed.
            if (!TryUpdateContext(x => referenceCounter = x))
                throw new ObjectDisposedException(nameof(ReferenceCountedDisposable<T>));
            return referenceCounter;
        }
    }
}
