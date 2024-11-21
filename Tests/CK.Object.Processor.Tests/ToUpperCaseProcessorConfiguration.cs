using CK.Core;
using System;
using System.Collections.Generic;

namespace CK.Object.Processor;

public sealed class ToUpperCaseProcessorConfiguration : ObjectProcessorConfiguration
{
    public ToUpperCaseProcessorConfiguration( IActivityMonitor monitor,
                                              TypedConfigurationBuilder builder,
                                              ImmutableConfigurationSection configuration,
                                              IReadOnlyList<ObjectProcessorConfiguration> processors )
        : base( monitor, builder, configuration, processors )
    {
        SetIntrinsicCondition( Condition );
        SetIntrinsicTransform( Transform );
    }

    Func<object, bool>? Condition( IServiceProvider services )
    {
        return static o => o is string;
    }

    Func<object, object>? Transform( IServiceProvider services )
    {
        return static o => ((string)o).ToUpperInvariant();
    }
}
