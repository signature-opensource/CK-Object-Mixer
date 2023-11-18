using CK.Core;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// The root abstraction is configured by an immutable configuration and can accept or reject 
    /// a IPoco and process it to produce any number of Poco instances.
    /// <para>
    /// Instances are are used by a single mix session: they can keep states and if they implement
    /// <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/> their dispose method will be
    /// called at the end of the session.
    /// </para>
    /// </summary>
    public abstract partial class BasePocoMixer
    {
        internal readonly PocoMixerConfiguration _configuration;
        [AllowNull] internal readonly PocoDirectory _pocoDirectory;

        /// <summary>
        /// Initializes a new <see cref="BasePocoMixer"/>.
        /// </summary>
        /// <param name="configuration">The required configuration.</param>
        protected BasePocoMixer( PocoMixerConfiguration configuration )
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public PocoMixerConfiguration Configuration => _configuration;

        /// <summary>
        /// Sets the <see cref="AcceptContext.Winner"/> to be this mixer with an optional state that will
        /// be provided to <see cref="ProcessAsync(IActivityMonitor, ProcessContext)"/>
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="acceptInfo">Optional state for the processing step.</param>
        protected void Accept( AcceptContext context, object? acceptInfo = null ) => context.Accept( this, acceptInfo );

        /// <summary>
        /// Rejects the current context for any <paramref name="reason"/> (including <see cref="RejectReason.None"/>).
        /// The <paramref name="context"/> is reset.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="reason">Rejection reason.</param>
        protected void Reject( AcceptContext context, RejectReason reason ) => context.Reject( this, reason );

        /// <summary>
        /// Must call <see cref="Accept(AcceptContext, object?)"/> or <see cref="Reject(AcceptContext, RejectReason)"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The accept context.</param>
        /// <returns>The awaitable.</returns>
        public abstract ValueTask AcceptAsync( IActivityMonitor monitor, AcceptContext context );

        /// <summary>
        /// Processes a previously accepted <see cref="ProcessContext.Input"/> by transforming it into 0 or more
        /// outputs sent to <see cref="ProcessContext.Output(IPoco)"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The process context.</param>
        /// <returns>The awaitable.</returns>
        public abstract ValueTask ProcessAsync( IActivityMonitor monitor, ProcessContext context );
    }

}
