using CK.Core;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Object.Mixer
{
    /// <summary>
    /// The root abstraction is configured by an immutable configuration and can accept or reject 
    /// an object and process it to produce any number of object instances.
    /// <para>
    /// Only <see cref="BaseObjectMixer{TConfiguration}"/> can be used as a base class. 
    /// </para>
    /// </summary>
    public abstract partial class BaseObjectMixer
    {
        readonly ObjectMixerConfiguration _configuration;

        private protected BaseObjectMixer( ObjectMixerConfiguration configuration )
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public ObjectMixerConfiguration Configuration => _configuration;

        /// <summary>
        /// Sets the <see cref="AcceptContext.Winner"/> to be this mixer with an optional state that will
        /// be provided to <see cref="DoProcessAsync(IActivityMonitor, ProcessContext)"/>
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
        /// Rejects the current context because of an exception.
        /// The exception is logged monitor and added to the user messages if a collector is available.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="ex">The exception.</param>
        protected void Reject( AcceptContext context, Exception ex ) => context.Reject( this, ex );

        /// <summary>
        /// Must call <see cref="Accept(AcceptContext, object?)"/> or <see cref="Reject(AcceptContext, RejectReason)"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The accept context.</param>
        /// <returns>The awaitable.</returns>
        internal protected abstract ValueTask AcceptAsync( IActivityMonitor monitor, AcceptContext context );

        /// <summary>
        /// Processes a previously accepted <see cref="ProcessContext.Input"/> by transforming it into 0 or more
        /// outputs sent to <see cref="ProcessContext.Output(object)"/>.
        /// <para>
        /// Errors must be signaled by calling <see cref="ProcessContext.SetError(IActivityMonitor, System.Exception?)"/>.
        /// </para>
        /// <para>
        /// Note that this method can route the call to another mixer with <see cref="ProcessContext.RouteAsync(IActivityMonitor, BaseObjectMixer)"/>.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="context">The process context.</param>
        /// <returns>The awaitable.</returns>
        internal protected abstract ValueTask ProcessAsync( IActivityMonitor monitor, ProcessContext context );

    }

}
