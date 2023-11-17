using CK.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Composite of multiple <see cref="IPocoMixer"/>.
    /// <para>
    /// Subordinate mixers' are called in depth-first order.
    /// </para>
    /// <para>
    /// This class may be specialized to override <see cref="RootAcceptAsync"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The target Poco type.</typeparam>
    public class CompositePocoMixer : PocoMixer<T, CompositePocoMixerConfiguration> where T : IPoco
    {
        readonly ImmutableArray<IPocoMixer<T>> _children;
        IPocoMixer<T>? _winner;

        internal CompositePocoMixer( CompositePocoMixerConfiguration configuration, ImmutableArray<IPocoMixer<T>> mixers )
            : base( configuration )
        {
            _children = mixers;
        }

        /// <summary>
        /// Calls <see cref="RootAcceptAsync"/> first and if it returns true (the default), submits the
        /// <paramref name="input"/> to all the subordinated mixers until the input is accepted.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="input">The input to challenge.</param>
        /// <param name="explanation">Optional explanations collector.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>True if the input should be processed by this mixer, false otherwise.</returns>
        public sealed override async ValueTask<bool> AcceptAsync( IActivityMonitor monitor,
                                                                  IPoco input,
                                                                  UserMessageCollector? explanation = null,
                                                                  CancellationToken cancellation = default )
        {
            if( !await RootAcceptAsync( monitor, input, explanation, cancellation ).ConfigureAwait( false ) )
            {
                return false;
            }
            foreach( var c in _children )
            {
                if( await c.AcceptAsync( monitor, input, explanation, cancellation ).ConfigureAwait( false ) )
                {
                    _winner = c;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Optional extension point that can be used to reject <paramref name="input"/> regardless
        /// of the subordinated mixer.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="input">The input to challenge.</param>
        /// <param name="explanation">Optional explanations collector.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>True if the input should be processed by this mixer, false otherwise.</returns>
        protected virtual ValueTask<bool> RootAcceptAsync( IActivityMonitor monitor,
                                                           IPoco input,
                                                           UserMessageCollector? explanation,
                                                           CancellationToken cancellation )
        {
            return new ValueTask<bool>( true );
        }

        /// <summary>
        /// Routes the call to the subordinate mixer that have accepted the <paramref name="input"/>.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="input">The accepted input.</param>
        /// <param name="output">Function to call to collect outputs.</param>
        /// <param name="cancellation">Cancellation token.</param>
        /// <returns>The awaitable.</returns>
        public sealed override ValueTask ProcessAsync( IActivityMonitor monitor,
                                                       IPoco input,
                                                       Action<IPoco> output,
                                                       CancellationToken cancellation )
        {
            Throw.DebugAssert( _winner != null );
            return _winner.ProcessAsync( monitor, input, output, cancellation );
        }
    }

}
