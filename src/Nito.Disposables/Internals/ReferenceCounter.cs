using System;
using System.Threading;

namespace Nito.Disposables.Internals;

/// <summary>
/// A reference count for an underlying target.
/// </summary>
public sealed class ReferenceCounter : IReferenceCounter
{
    private object? _target;
    private int _count;

    /// <summary>
    /// Creates a new reference counter with a reference count of 1 referencing the specified target.
    /// </summary>
    public ReferenceCounter(object? target)
    {
        _target = target;
        _count = 1;
    }

    bool IReferenceCounter.TryIncrementCount() => TryUpdate(x => x + 1) != null;

    object? IReferenceCounter.TryDecrementCount()
    {
        var updateResult = TryUpdate(x => x - 1);
        if (updateResult != 0)
            return null;
        return Interlocked.Exchange(ref _target, null);
    }

    object? IReferenceCounter.TryGetTarget()
    {
        var result = Interlocked.CompareExchange(ref _target, null, null);
        var count = Interlocked.CompareExchange(ref _count, 0, 0);
        if (count == 0)
            return null;
        return result;
    }

    private int? TryUpdate(Func<int, int> func)
    {
        while (true)
        {
            var original = Interlocked.CompareExchange(ref _count, 0, 0);
            if (original == 0)
                return null;
            var updatedCount = func(original);
            var result = Interlocked.CompareExchange(ref _count, updatedCount, original);
            if (original == result)
                return updatedCount;
        }
    }
}
