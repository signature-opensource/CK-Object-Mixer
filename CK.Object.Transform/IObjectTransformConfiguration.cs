using CK.Core;

namespace CK.Object.Transform;

/// <summary>
/// Minimal view of a transform configuration.
/// </summary>
public interface IObjectTransformConfiguration
{
    /// <summary>
    /// Gets the configuration path.
    /// </summary>
    string ConfigurationPath { get; }
}
