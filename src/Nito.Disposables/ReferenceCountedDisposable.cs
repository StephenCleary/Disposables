using System;
using Nito.Disposables.Internals;

namespace Nito.Disposables
{

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
}
