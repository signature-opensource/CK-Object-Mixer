using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Poco.Mixer
{
    public sealed class SimplePocoMixer<T> where T : class, IPoco
    {
        readonly IServiceProvider _services;
        readonly PocoMixerConfiguration _configuration;
        // At least 1 when mixer is initialized.
        int _maxMixCount;
        BasePocoMixer? _mixer;

        public SimplePocoMixer( IServiceProvider services, PocoMixerConfiguration configuration )
        {
            Throw.CheckNotNullArgument( services );
            Throw.CheckNotNullArgument( configuration );
            _services = services;
            _configuration = configuration;
        }

        public async ValueTask<MixerResult<T>> MixAsync( IActivityMonitor monitor, IPoco input, UserMessageCollector? userMessages, CancellationToken cancellation )
        {
            var mixer = EnsureInitialized( monitor );
            if( mixer == null )
            {
                userMessages?.Error( $"Empty mixer configured by '{_configuration.Name}'." );
                return new MixerResult<T>( false );
            }
            var processor = new MixerProcessor( mixer, typeof(T), userMessages, _maxMixCount, cancellation );
            return await processor.ProcessAsync( monitor, input );
        }

        BasePocoMixer? EnsureInitialized( IActivityMonitor monitor )
        {
            if( _maxMixCount == 0 )
            {
                _mixer = _configuration.CreateMixer( monitor, _services );
                if( _mixer == null )
                {
                    _maxMixCount = 1;
                    monitor.Error( $"Empty mixer configured by '{_configuration.Name}'." );
                }
                else
                {
                    var max = _configuration.Configuration.TryGetIntValue( monitor, "MaxMixCount", 1 );
                    if( max.HasValue ) _maxMixCount = max.Value;
                    else
                    {
                        _maxMixCount = 100;
                        monitor.Info( $"Mixer '{_configuration.Name}' use the default MaxMixCount = 100." );
                    }
                }
            }
            return _mixer;
        }
    }
}
