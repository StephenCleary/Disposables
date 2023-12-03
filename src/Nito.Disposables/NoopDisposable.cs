using System;
using System.Threading.Tasks;

namespace Nito.Disposables;

/// <summary>
/// A singleton disposable that does nothing when disposed.
/// </summary>
public sealed class NoopDisposable: IDisposable
#if !NETSTANDARD1_0 && !NETSTANDARD2_0 && !NET461
    , IAsyncDisposable
#endif
{
    private NoopDisposable()
    {
    }

    /// <summary>
    /// Does nothing.
    /// </summary>
    public void Dispose()
    {
    }

#if !NETSTANDARD1_0 && !NETSTANDARD2_0 && !NET461
    /// <summary>
	/// Does nothing.
	/// </summary>
	public ValueTask DisposeAsync() => new ValueTask();
#endif

    /// <summary>
    /// Gets the instance of <see cref="NoopDisposable"/>.
    /// </summary>
    public static NoopDisposable Instance { get; } = new NoopDisposable();
}
