#if !NETSTANDARD1_0 && !NETSTANDARD2_0 && !NET461
using System;
using System.Collections.Generic;
using System.Text;

namespace Nito.Disposables.Internals;

/// <summary>
/// An instance that represents an uncounted weak reference.
/// </summary>
public sealed class WeakReferenceCountedAsyncDisposable<T> : IWeakReferenceCountedAsyncDisposable<T>
    where T : class, IAsyncDisposable
{
    private readonly WeakReference<IReferenceCounter> _weakReference;

    /// <summary>
    /// Creates an instance that weakly references the specified reference counter. The specified reference counter should not be incremented.
    /// </summary>
    public WeakReferenceCountedAsyncDisposable(IReferenceCounter referenceCounter)
    {
        _ = referenceCounter ?? throw new ArgumentNullException(nameof(referenceCounter));

        _weakReference = new(referenceCounter);

        // Ensure we can cast from the stored disposable to T.
        _ = (T?) referenceCounter.TryGetTarget()!;
    }

    IReferenceCountedAsyncDisposable<T>? IWeakReferenceCountedAsyncDisposable<T>.TryAddReference()
    {
        if (!_weakReference.TryGetTarget(out var referenceCounter))
            return null;
        if (!referenceCounter.TryIncrementCount())
            return null;
        return new ReferenceCountedAsyncDisposable<T>(referenceCounter);
    }

    T? IWeakReferenceCountedAsyncDisposable<T>.TryGetTarget()
    {
        if (!_weakReference.TryGetTarget(out var referenceCounter))
            return null;
        return (T?) referenceCounter.TryGetTarget();
    }
}
#endif