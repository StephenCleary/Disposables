#if NETSTANDARD2_1
using System;
using System.Runtime.CompilerServices;
using Nito.Disposables.Internals;

namespace Nito.Disposables
{
    /// <summary>
    /// Creation methods for reference counted disposables.
    /// </summary>
    public static class ReferenceCountedAsyncDisposable
    {
        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed. This method uses attached (ephemeron) reference counters.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then the returned instance does nothing when it is disposed.</param>
        public static IReferenceCountedAsyncDisposable<T> Create<T>(T? disposable)
            where T : class, IAsyncDisposable =>
            TryCreate(disposable) ?? throw new ObjectDisposedException(nameof(T));

        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed. This method uses attached (ephemeron) reference counters.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then the returned instance does nothing when it is disposed.</param>
        public static IReferenceCountedAsyncDisposable<T>? TryCreate<T>(T? disposable)
            where T : class, IAsyncDisposable
        {
            // We can't attach reference counters to null, so we use a sort of null object pattern here.
            if (disposable == null)
                return CreateWithNewReferenceCounter(disposable);

            var referenceCounter = ReferenceCounterEphemerons.TryGetAndIncrementOrCreate(disposable);
            if (referenceCounter == null)
                return null;

            return new ReferenceCountedAsyncDisposable<T>(referenceCounter);
        }

        /// <summary>
        /// Creates a new disposable that disposes <paramref name="disposable"/> when all reference counts have been disposed. This method creates a new reference counter to keep track of the reference counts.
        /// </summary>
        /// <param name="disposable">The disposable to dispose when all references have been disposed. If this is <c>null</c>, then the returned instance does nothing when it is disposed.</param>
        public static IReferenceCountedAsyncDisposable<T> CreateWithNewReferenceCounter<T>(T? disposable)
            where T : class, IAsyncDisposable
            => new ReferenceCountedAsyncDisposable<T>(new ReferenceCounter(disposable));
    }
}
#endif