using CK.Core;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Root implementation of all mixers.
    /// </summary>
    /// <typeparam name="TConfiguration"></typeparam>
    public abstract class BasePocoMixer<TConfiguration> : BasePocoMixer where TConfiguration : PocoMixerConfiguration
    {
        /// <summary>
        /// Initializes a new <see cref="BasePocoMixer"/>.
        /// </summary>
        /// <param name="configuration">The required configuration.</param>
        protected BasePocoMixer( TConfiguration configuration )
            : base( configuration )
        {
        }

        /// <inheritdoc />
        public new TConfiguration Configuration => Unsafe.As<TConfiguration>( _configuration );

        internal override ValueTask DoProcessAsync( IActivityMonitor monitor, ProcessContext context )
        {
            var previous = context.SetCurrentMixer( this );
            try
            {
                return ProcessAsync( monitor, context );
            }
            finally
            {
                context.SetCurrentMixer( previous );
            }
        }

        /// <summary>
        /// Processes a previously accepted <see cref="ProcessContext.Input"/> by transforming it into 0 or more
        /// outputs sent to <see cref="ProcessContext.Output(IPoco)"/>.
        /// <para>
        /// Errors must be signaled by calling <see cref="BasePocoMixer.ProcessContext.SetError(IActivityMonitor, System.Exception?)"/>.
        /// </para>
        /// <para>
        /// Note that this method can call other <see cref="BasePocoMixer.DoProcessAsync(IActivityMonitor, ProcessContext)"/>.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The process context.</param>
        /// <returns>The awaitable.</returns>
        protected abstract ValueTask ProcessAsync( IActivityMonitor monitor, ProcessContext context );
    }
}
