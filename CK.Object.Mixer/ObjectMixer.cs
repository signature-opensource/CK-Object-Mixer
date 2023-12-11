using CK.Core;
using CK.Object.Predicate;
using CK.Object.Processor;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace CK.Object.Mixer
{
    /// <summary>
    /// Instance mixer created by <see cref="ObjectMixerConfiguration.Create(IServiceProvider)"/>.
    /// <para>
    /// This concrete base class is operational, providing <see cref="ObjectMixerConfiguration.Processor"/>
    /// and/or <see cref="ObjectMixerConfiguration.OutputCondition"/> exist. When they don't, this mixer
    /// does nothing: it is an identity function.
    /// </para>
    /// <para>
    /// By overridding <see cref="ProcessAsync(IActivityMonitor, object)"/> and <see cref="FilterOutputAsync(IActivityMonitor, object)"/>,
    /// its behavior can totally change (including ignoring the processor and output condition from the configuration but this is not
    /// recommended).
    /// </para>
    /// </summary>
    public partial class ObjectMixer : IObjectMixer<object>
    {
        readonly IServiceProvider _services;
        readonly ObjectMixerConfiguration _configuration;
        readonly Func<object, ValueTask<object?>>? _configuredProcessor;
        readonly Func<object, ValueTask<bool>>? _configuredOutputCondition;
        bool _errorInConfiguredProcessor;
        bool _errorInConfiguredCondition;

        /// <summary>
        /// Initializes a new <see cref="ObjectMixer"/>.
        /// </summary>
        /// <param name="services">Services provider for predicates, transforms or processors that may depend on services.</param>
        /// <param name="configuration">Mixer configuration.</param>
        internal protected ObjectMixer( IServiceProvider services, ObjectMixerConfiguration configuration )
        {
            _services = services;
            _configuration = configuration;
            _configuredProcessor = _configuration.Processor?.CreateAsyncProcessor( services );
            _configuredOutputCondition = _configuration.OutputCondition?.CreateAsyncPredicate( services );
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public ObjectMixerConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the processor function of the <see cref="ObjectMixerConfiguration.Processor"/> if any.
        /// </summary>
        protected Func<object, ValueTask<object?>>? ConfiguredProcessor => _configuredProcessor;

        /// <summary>
        /// Gets the predicate of the <see cref="ObjectMixerConfiguration.OutputCondition"/> if any.
        /// </summary>
        protected Func<object, ValueTask<bool>>? ConfiguredOutputCondition => _configuredOutputCondition;

        /// <summary>
        /// Mixes the <paramref name="input"/>.
        /// <para>
        /// This method cannot be overridden: <see cref="ProcessAsync(IActivityMonitor, object)"/> and
        /// <see cref="FilterOutputAsync(IActivityMonitor, object)"/> are the extension points.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="input">The input to mix.</param>
        /// <returns>The result of the mix.</returns>
        public Task<IObjectMixerResult<object>> MixAsync( IActivityMonitor monitor, object input )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( input );
            return new Mixer( monitor, this, input, _configuration.MaxProcessCount, _configuration.OutputType ).GetResultAsync();
        }

        /// <summary>
        /// Applies the configured <see cref="ObjectMixerConfiguration.Processor"/> if any.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="input">The input object.</param>
        /// <returns>The transformation result.</returns>
        protected virtual async ValueTask<object?> ProcessAsync( IActivityMonitor monitor, object input )
        {
            if( _configuredProcessor != null )
            {
                // We can avoid a try/catch here.
                _errorInConfiguredProcessor = true;
                var r = await _configuredProcessor( input );
                _errorInConfiguredProcessor = false;
                return r;
            }
            return input;
        }

        /// <summary>
        /// Challenges the configured <see cref="ObjectMixerConfiguration.OutputCondition"/> if any.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="input">The input object.</param>
        /// <returns>Whether the object is a valid output for this mixer.</returns>
        protected virtual async ValueTask<bool> FilterOutputAsync( IActivityMonitor monitor, object input )
        {
            if( _configuredOutputCondition != null )
            {
                // We can avoid a try/catch here.
                _errorInConfiguredCondition = true;
                var r = await _configuredOutputCondition( input );
                _errorInConfiguredCondition = false;
                return r;
            }
            return true;
        }
    }
}
