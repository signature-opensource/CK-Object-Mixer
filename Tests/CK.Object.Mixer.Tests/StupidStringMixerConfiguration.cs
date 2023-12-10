using CK.Core;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace CK.Object.Mixer
{
    public class StupidStringMixerConfiguration : ObjectMixerConfiguration
    {
        readonly bool _ensureProcessed;

        public StupidStringMixerConfiguration( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )
            : base( monitor, builder, configuration )
        {
            _ensureProcessed = configuration.TryGetBooleanValue( monitor, "EnsureProcessed" ) ?? false;
        }

        public override Type OutputType => typeof(string);

        public override ObjectMixer Create( IServiceProvider services )
        {
            return new Mixer( services, this, _ensureProcessed );
        }

        sealed class Mixer : ObjectMixer
        {
            readonly bool _ensureProcessed;

            internal Mixer( IServiceProvider services, ObjectMixerConfiguration configuration, bool ensureProcessed )
                : base( services, configuration )
            {
                _ensureProcessed = ensureProcessed;
            }

            protected override async ValueTask<object?> ProcessAsync( IActivityMonitor monitor, object input )
            {
                var o = await base.ProcessAsync( monitor, input );
                if( o == null ) return null;
                if( o is string s )
                {
                    return !s.StartsWith("Processed<") ? $"Processed<{s}>" : s;
                }
                return ImmutableArray.Create( o.ToString(), o.GetType().ToCSharpName() );
            }

            protected override async ValueTask<bool> FilterOutputAsync( IActivityMonitor monitor, object input )
            {
                Throw.DebugAssert( input is string );
                if( _ensureProcessed && !((string)input).StartsWith( "Processed<" ) ) return false;
                return await base.FilterOutputAsync( monitor, input );
            }

        }

    }
}
