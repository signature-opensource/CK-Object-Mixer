using System.Collections.Immutable;

namespace CK.Object.Transform
{
    /// <summary>
    /// Generalizes <see cref="SequenceTransformHook"/> and <see cref="SequenceAsyncTransformHook"/> wrappers.
    /// </summary>
    public interface ISequenceTransformHook : IObjectTransformHook
    {
        /// <summary>
        /// Gets the subordinated transformations.
        /// </summary>
        ImmutableArray<IObjectTransformHook> Transforms { get; }
    }

}
