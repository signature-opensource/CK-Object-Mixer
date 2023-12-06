using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Generalizes <see cref="ObjectTransformHook"/> and <see cref="ObjectAsyncTransformHook"/>.
    /// </summary>
    public interface IObjectTransformHook
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IObjectTransformConfiguration Configuration { get; }

        /// <summary>
        /// Gets this hook as a synchronous one if the transform is synchronous.
        /// </summary>
        ObjectTransformHook? Synchronous { get; }

        /// <summary>
        /// Gets the hook context to which this hook is bound.
        /// </summary>
        TransformHookContext Context { get; }

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The transformed result.</returns>
        ValueTask<object> TransformAsync( object o );


    }

}
