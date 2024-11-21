using System.Collections.Generic;

namespace CK.Object.Transform;

/// <summary>
/// The composite of transformation is a simple sequence of <see cref="Transforms"/>.
/// </summary>
public interface ISequenceTransformConfiguration : IObjectTransformConfiguration
{
    /// <summary>
    /// Gets the subordinated transform configurations.
    /// <para>
    /// When this is empty, this configuration generates the (null) identity transformation.
    /// Note that this is only configurations. Each of them can generate a null identity transformation:
    /// items in this list doens't guaranty anything about the eventual transformation. 
    /// </para>
    /// </summary>
    IReadOnlyList<ObjectAsyncTransformConfiguration> Transforms { get; }
}
