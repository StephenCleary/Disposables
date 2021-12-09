﻿using System;
using System.Threading;

namespace Nito.Disposables.Internals
{
    /// <summary>
    /// Default implementation of <see cref="IReferenceCounter{T}"/>
    /// </summary>
    public sealed class ReferenceCounter<T> : IReferenceCounter<T>
        where T : class, IDisposable
    {
        private T? _disposable;
        private int _count;

        /// <summary>
        /// Creates a new reference counter with a reference count of 1 referencing the specified disposable.
        /// </summary>
        /// <param name="disposable"></param>
        public ReferenceCounter(T? disposable)
        {
            _disposable = disposable;
            _count = 1;
        }

        bool IReferenceCounter<T>.TryIncrementCount() => TryUpdate(x => x == 0 ? null : x + 1) != null;

        IDisposable? IReferenceCounter<T>.TryDecrementCount()
        {
            var updateResult = TryUpdate(x => x == 0 ? null : x - 1);
            if (updateResult != 0)
                return null;
            return Interlocked.Exchange(ref _disposable, null);
        }

        T? IReferenceCounter<T>.TryGetTarget()
        {
            var result = Interlocked.CompareExchange(ref _disposable, null, null);
            var count = Interlocked.CompareExchange(ref _count, 0, 0);
            if (count == 0)
                return null;
            return result;
        }

        private int? TryUpdate(Func<int, int?> func)
        {
            while (true)
            {
                var original = Interlocked.CompareExchange(ref _count, 0, 0);
                if (original == 0)
                    return null;
                var updatedCount = func(original);
                if (updatedCount == null)
                    return null;
                var result = Interlocked.CompareExchange(ref _count, updatedCount.Value, original);
                if (original == result)
                    return updatedCount.Value;
            }
        }
    }
}