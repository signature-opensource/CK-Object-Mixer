using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Object.Mixer
{
    /// <summary>
    /// Mixer instance obtained by <see cref="ObjectMixerFactory.Create{T}(IActivityMonitor, IServiceProvider)"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ObjectMixer<T> where T : class
    {
        readonly ObjectMixerFactory _factory;
        readonly BaseObjectMixer? _mixer;

        internal ObjectMixer( ObjectMixerFactory factory, BaseObjectMixer? mixer )
        {
            _factory = factory;
            _mixer = mixer;
        }

        /// <summary>
        /// Gets whether this mixer is empty.
        /// <see cref="MixAsync(IActivityMonitor, object, UserMessageCollector?, CancellationToken)"/> will always
        /// return a false <see cref="ObjectMixerResult{T}.Success"/>.
        /// </summary>
        public bool IsEmpty => _mixer == null;

        /// <summary>
        /// Mixes a seed input.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="input">The input seed.</param>
        /// <param name="userMessages">Optional user messages collector.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>A mixer result.</returns>
        public async ValueTask<ObjectMixerResult<T>> MixAsync( IActivityMonitor monitor,
                                                               object input,
                                                               UserMessageCollector? userMessages,
                                                               CancellationToken cancellation = default )
        {
            if( _mixer == null )
            {
                userMessages?.Error( $"Empty mixer configured by '{_factory.RootConfiguration.Configuration.Path}'." );
                return new ObjectMixerResult<T>( userMessages );
            }
            var processor = new ObjectMixerProcessor( _mixer, userMessages, _factory, cancellation );
            var untyped = await processor.ProcessAsync( monitor, input );
            return new ObjectMixerResult<T>( untyped );
        }

    }
}
