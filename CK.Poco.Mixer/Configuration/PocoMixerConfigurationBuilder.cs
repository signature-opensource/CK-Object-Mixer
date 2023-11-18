using CK.Core;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Singleton service that can create a <see cref="PocoMixerConfiguration"/> from a <see cref="IConfigurationSection"/>.
    /// </summary>
    public sealed class PocoMixerConfigurationBuilder : ISingletonAutoService
    {
        PolymorphicConfigurationTypeBuilder? _cachedBuilder;

        /// <summary>
        /// Creates a mixer configuration.
        /// </summary>
        /// <param name="monitor">The monitor used to signal errors and warnings.</param>
        /// <param name="configuration">The configuration section to analyze.</param>
        /// <returns>The configigration or null on error.</returns>
        public PocoMixerConfiguration? Create( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            // No real need for a try/finally here.
            var b = ObtainBuilder();
            var r = b.Create<PocoMixerConfiguration>( monitor, configuration );
            ReleaseBuilder( b );
            return r;
        }

        PolymorphicConfigurationTypeBuilder ObtainBuilder()
        {
            var cached = Interlocked.Exchange( ref _cachedBuilder, null );
            if( cached == null )
            {
                cached = new PolymorphicConfigurationTypeBuilder();
                cached.AddStandardTypeResolver( baseType: typeof( PocoMixerConfiguration ),
                                                typeNamespace: "CK.Poco.Mixer",
                                                allowOtherNamespace: false,
                                                familyTypeNameSuffix: "PocoMixer",
                                                compositeBaseType: typeof( CompositePocoMixer ),
                                                compositeItemsFieldName: "Mixers" );
            }
            return cached;
        }

        void ReleaseBuilder( PolymorphicConfigurationTypeBuilder b )
        {
            Interlocked.CompareExchange( ref _cachedBuilder, b, null );
        }

    }

}
