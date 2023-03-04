using System.Runtime.CompilerServices;

namespace Nito.Disposables.Internals;

/// <summary>
/// The collection of reference counters, stored as ephemerons.
/// </summary>
public static class ReferenceCounterEphemerons
{
    /// <summary>
    /// Increments and returns the reference counter for the specified target, creating it if necessary.
    /// Returns <c>null</c> if the reference counter has already reached 0.
    /// </summary>
    public static IReferenceCounter? TryGetAndIncrementOrCreate(object target)
    {
        ReferenceCounter? createdReferenceCounter = null;
        var referenceCounter =  Ephemerons.GetValue(target, t => createdReferenceCounter = new ReferenceCounter(t));
        if (referenceCounter != createdReferenceCounter)
        {
            if (!referenceCounter.TryIncrementCount())
                return null;
        }

        return referenceCounter;
    }

    private static readonly ConditionalWeakTable<object, IReferenceCounter> Ephemerons = new();
}
