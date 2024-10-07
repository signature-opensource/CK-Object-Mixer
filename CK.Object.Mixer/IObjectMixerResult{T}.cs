using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CK.Core;

namespace CK.Object.Mixer;


/// <summary>
/// Captures the result of <see cref="IObjectMixer{T}.MixAsync(IActivityMonitor, object)"/>.
/// </summary>
/// <typeparam name="T">The mixer's output type.</typeparam>
public interface IObjectMixerResult<out T> where T : class
{
    /// <summary>
    /// Gets the exception if one has been thrown.
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// Gets the total number of processes that have been executed.
    /// </summary>
    int TotalProcessCount { get; }

    /// <summary>
    /// Gets the input seed.
    /// </summary>
    object Input { get; }

    /// <summary>
    /// Gets the outputs of the mix.
    /// </summary>
    IReadOnlyList<T> Output { get; }

    /// <summary>
    /// Gets the rejected objects.
    /// </summary>
    ImmutableArray<(object Object, string Reason)> Rejected { get; }
}
