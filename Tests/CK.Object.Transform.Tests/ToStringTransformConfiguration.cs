using CK.Core;
using System;

namespace CK.Object.Transform;

public sealed partial class ToStringTransformConfiguration : ObjectTransformConfiguration
{
    public ToStringTransformConfiguration( IActivityMonitor monitor,
                                           TypedConfigurationBuilder builder,
                                           ImmutableConfigurationSection configuration )
        : base( configuration.Path )
    {
    }

    public override Func<object, object>? CreateTransform( IServiceProvider services )
    {
        return static o => o.ToString() ?? "<null>";
    }
}

