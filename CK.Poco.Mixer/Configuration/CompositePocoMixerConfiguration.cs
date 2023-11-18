using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Poco.Mixer
{
    public class CompositePocoMixerConfiguration : PocoMixerConfiguration
    {
        readonly IReadOnlyList<PocoMixerConfiguration> _mixers;

        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder. Can be used to instantiate other children as needed.</param>
        /// <param name="configuration">The configuration for this object.</param>
        /// <param name="strategies">The subordinated items.</param>
        public CompositePocoMixerConfiguration( IActivityMonitor monitor,
                                                PolymorphicConfigurationTypeBuilder builder,
                                                ImmutableConfigurationSection configuration,
                                                IReadOnlyList<PocoMixerConfiguration> mixers )
            : base( monitor, builder, configuration )
        {
            _mixers = mixers;
        }

        /// <inheritdoc />
        public override BasePocoMixer? CreateMixer( IActivityMonitor monitor, IServiceProvider services )
        {
            var items = CreateMixers( monitor, services );
            return items.Length > 0
                    ? new CompositePocoMixer( this, items! )
                    : null;
        }

        protected ImmutableArray<BasePocoMixer> CreateMixers( IActivityMonitor monitor, IServiceProvider services )
        {
            return _mixers.Select( c => c.CreateMixer( monitor, services ) )
                          .Where( s => s != null )
                          .ToImmutableArray()!;
        }

    }

}
