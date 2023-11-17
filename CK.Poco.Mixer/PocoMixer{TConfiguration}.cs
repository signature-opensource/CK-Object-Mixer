using CK.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Root implementation of all mixers.
    /// </summary>
    /// <typeparam name="TConfiguration"></typeparam>
    public abstract class PocoMixer<TConfiguration> : PocoMixer where TConfiguration : PocoMixerConfiguration
    {
        readonly TConfiguration _configuration;

        protected PocoMixer( TConfiguration configuration )
        {
            _configuration = configuration;
        }

        PocoMixerConfiguration IPocoMixer.Configuration => _configuration;

        /// <inheritdoc />
        public TConfiguration Configuration => _configuration;


        /// <inheritdoc />
        public abstract ValueTask<bool> AcceptAsync( IActivityMonitor monitor,
                                                     IPoco input,
                                                     UserMessageCollector? explanation,
                                                     CancellationToken cancellation );

        /// <inheritdoc />
        public abstract ValueTask ProcessAsync( IActivityMonitor monitor,
                                                IPoco input,
                                                Action<IPoco> output,
                                                CancellationToken cancellation = default );
    }

}
