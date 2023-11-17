using CK.Core;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace CK.Poco.Mixer
{
    public sealed class PocoMixerConfigurationBuilder : ISingletonAutoService
    {
        readonly PocoDirectory _pocoDirectory;
        ConfigurationBuilder? _cachedBuilder;

        sealed class ConfigurationBuilder : PolymorphicConfigurationTypeBuilder
        {
            readonly PocoDirectory _pocoDirectory;

            public ConfigurationBuilder( PocoDirectory pocoDirectory )
            {
                _pocoDirectory = pocoDirectory;
            }

            public PocoDirectory PocoDirectory => _pocoDirectory;
        }

        public PocoMixerConfigurationBuilder( PocoDirectory pocoDirectory )
        {
            _pocoDirectory = pocoDirectory;
        }

        ConfigurationBuilder ObtainBuilder()
        {
            var cached = Interlocked.Exchange( ref _cachedBuilder, null );
            if( cached == null )
            {
                cached = new ConfigurationBuilder( _pocoDirectory );
                cached.AddStandardTypeResolver( baseType: typeof( PocoMixerConfiguration ),
                                                typeNamespace: "CK.Poco.Mixer",
                                                allowOtherNamespace: false,
                                                familyTypeNameSuffix: "PocoMixer",
                                                compositeBaseType: typeof( CompositePocoMixer ),
                                                compositeItemsFieldName: "Mixers" );
            }
            return cached;
        }

        void ReleaseBuilder( ConfigurationBuilder b )
        {
            Interlocked.CompareExchange( ref _cachedBuilder, b, null );
        }

        public PocoMixerConfiguration? Create( IActivityMonitor monitor, IConfiguration configuration )
        {
            // No real need for a try/finally here.
            var b = ObtainBuilder();
            var r = b.Create<PocoMixerConfiguration>( monitor, configuration );
            ReleaseBuilder( b );
            return r;
        }
    }

    /// <summary>
    /// The root abstraction is configured by an immutable configuration and can accept or reject 
    /// a IPoco and process it to produce any number of Poco instances.
    /// <para>
    /// Instances are are used by a single mix session: they can keep states and if they implement
    /// <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/> their dispose method will be
    /// called at the end of the session.
    /// </para>
    /// </summary>
    public abstract class PocoMixer
    {
        internal readonly PocoMixerConfiguration _configuration;
        [AllowNull] internal readonly PocoDirectory _pocoDirectory;

        protected PocoMixer( PocoMixerConfiguration configuration )
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public PocoMixerConfiguration Configuration => _configuration;

        /// <summary>
        /// Accepts or rejects the <paramref name="input"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="input">The input to challenge.</param>
        /// <param name="explanation">Optional explanations collector.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>True if the input should be processed by this mixer, false otherwise.</returns>
        public abstract ValueTask<bool> AcceptAsync( IActivityMonitor monitor,
                                                     IPoco input,
                                                     UserMessageCollector? explanation = null,
                                                     CancellationToken cancellation = default );

        /// <summary>
        /// Processes a previously accepted <paramref name="input"/> by transforming it into 0 or more
        /// outputs sent to <paramref name="output"/> collector.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="input">The accepted input.</param>
        /// <param name="output">Function to call to collect outputs.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>The awaitable.</returns>
        public abstract ValueTask ProcessAsync( IActivityMonitor monitor,
                                                IPoco input,
                                                Action<IPoco> output,
                                                CancellationToken cancellation = default );
    }

}
