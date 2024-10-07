using CK.Core;
using System;

namespace CK.Object.Predicate;

/// <summary>
/// Simple always true object predicate.
/// </summary>
public sealed class AlwaysTruePredicateConfiguration : ObjectPredicateConfiguration
{
    /// <summary>
    /// Required constructor.
    /// </summary>
    /// <param name="monitor">Unused monitor.</param>
    /// <param name="builder">Unused builder.</param>
    /// <param name="configuration">Captured configuration.</param>
    public AlwaysTruePredicateConfiguration( IActivityMonitor monitor,
                                             TypedConfigurationBuilder builder,
                                             ImmutableConfigurationSection configuration )
        : base( configuration.Path )
    {
    }

    /// <inheritdoc />
    public override Func<object, bool> CreatePredicate( IServiceProvider services )
    {
        return static _ => true;
    }
}
