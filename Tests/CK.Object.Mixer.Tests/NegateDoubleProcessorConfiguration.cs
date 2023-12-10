using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Collections.Generic;

namespace CK.Object.Processor
{
    public sealed class NegateDoubleProcessorConfiguration : ObjectProcessorConfiguration
    {
        public NegateDoubleProcessorConfiguration( IActivityMonitor monitor,
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
            return static o => o is double;
        }

        Func<object, object>? Transform( IServiceProvider services )
        {
            return static o => -((double)o);
        }
    }

}
