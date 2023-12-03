#if !NETSTANDARD1_0 && !NETSTANDARD2_0 && !NET461
using System;

namespace Nito.Disposables;

/// <summary>
/// An instance that represents an uncounted weak reference. All members are threadsafe.
/// </summary>
public interface IWeakReferenceCountedAsyncDisposable<out T>
    where T : class, IAsyncDisposable
{
    /// <summary>
    /// Adds a reference to this reference counted disposable. Returns <c>null</c> if the underlying disposable has already been disposed or garbage collected.
    /// </summary>
    IReferenceCountedAsyncDisposable<T>? TryAddReference();

    /// <summary>
    /// Attempts to get the target object. Returns <c>null</c> if the underlying disposable has already been disposed or garbage collected.
    /// </summary>
    T? TryGetTarget();
}
#endif