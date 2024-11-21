using CK.Core;
using System.Threading.Tasks;

namespace CK.Object.Mixer;

/// <summary>
/// Mixer with a typed output.
/// <para>
/// This unfortunately cannot be covariant on <typeparamref name="T"/> because of the Task that
/// is invariant.
/// </para>
/// </summary>
/// <typeparam name="T">The mixer's output type.</typeparam>
public interface IObjectMixer<T> where T : class
{
    /// <summary>
    /// Gets the configuration.
    /// </summary>
    ObjectMixerConfiguration Configuration { get; }

    /// <summary>
    /// Mixes the <paramref name="input"/> into a <see cref="IObjectMixerResult{T}"/>.
    /// </summary>
    /// <param name="monitor">The monitor.</param>
    /// <param name="input">The input to mix.</param>
    /// <returns>The result of the mix.</returns>
    Task<IObjectMixerResult<T>> MixAsync( IActivityMonitor monitor, object input );

}
