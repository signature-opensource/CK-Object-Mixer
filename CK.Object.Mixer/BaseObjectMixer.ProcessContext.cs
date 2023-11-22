using CK.Core;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CK.Object.Mixer
{
    public abstract partial class BaseObjectMixer
    {
        /// <summary>
        /// Context provided to <see cref="DoProcessAsync(IActivityMonitor, ProcessContext)"/>.
        /// </summary>
        public sealed class ProcessContext
        {
            [AllowNull] object _input;
            [AllowNull] UserMessageCollector? _userMessages;
            [AllowNull] CancellationToken _cancellation;
            [AllowNull] ObjectMixerFactory _factory;
            [AllowNull] object? _acceptInfo;
            [AllowNull] Action<ProcessContext, object> _output;
            [AllowNull] BaseObjectMixer _mixer;
            bool _hasError;

            internal void Initialize( AcceptContext a, Action<ProcessContext, object> output )
            {
                _input = a.Input;
                _userMessages = a.UserMessages;
                _cancellation = a.Cancellation;
                _factory = a.Factory;
                _acceptInfo = a._acceptInfo;
                _output = output;
                _mixer = a._winner;
            }

            /// <summary>
            /// Gets the input that must be processed.
            /// </summary>
            public object Input => _input;

            /// <summary>
            /// Gets the optional user message collector.
            /// </summary>
            public UserMessageCollector? UserMessages => _userMessages;

            /// <summary>
            /// Gets the cancellation token.
            /// </summary>
            public CancellationToken Cancellation => _cancellation;

            /// <summary>
            /// Gets the state that <see cref="BaseObjectMixer.Accept(AcceptContext, object?)"/>
            /// may have set.
            /// </summary>
            public object? AcceptInfo => _acceptInfo;

            /// <summary>
            /// Gets whether <see cref="SetError(Exception?)"/> has been called.
            /// </summary>
            public bool HasError => _hasError;

            /// <summary>
            /// Sets an error. Handles log to the monitor and to <see cref="UserMessages"/>.
            /// </summary>
            /// <param name="monitor">The monitor.</param>
            /// <param name="error">Optional exception that causes the error.</param>
            public void SetError( IActivityMonitor monitor, Exception? error = null )
            {
                // This will always log, even if error is null and if error is not null,
                // the error is appended to the user messages (if any).
                AcceptContext.EmitError( monitor, _factory, _userMessages, _input, _mixer, error );
                // When error is null, we ensure that at least one error user message has been emitted.
                if( error == null && !_hasError && _userMessages != null )
                {
                    _userMessages.Error( $"Mixer '{_factory.GetMixerName( _mixer.Configuration )}' encountered an error." );
                }
                _hasError = true;
            }

            /// <summary>
            /// Outputs a result that may be an intermediate result that requires
            /// a subsequent processing.
            /// </summary>
            /// <param name="output">The result.</param>
            public void Output( object output )
            {
                Throw.CheckNotNullArgument( output );
                _output( this, output );
            }
        }
    }

}
