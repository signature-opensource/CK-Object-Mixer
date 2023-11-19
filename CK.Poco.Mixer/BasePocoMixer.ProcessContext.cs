using CK.Core;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Poco.Mixer
{
    public abstract partial class BasePocoMixer
    {
        /// <summary>
        /// Context provided to <see cref="DoProcessAsync(IActivityMonitor, ProcessContext)"/>.
        /// </summary>
        public sealed class ProcessContext
        {
            [AllowNull] IPoco _input;
            [AllowNull] UserMessageCollector? _userMessages;
            [AllowNull] CancellationToken _cancellation;
            [AllowNull] object? _acceptInfo;
            [AllowNull] Action<IPoco> _output;
            [AllowNull] BasePocoMixer _mixer;
            bool _hasError;

            internal void Initialize( AcceptContext a, Action<IPoco> output )
            {
                _input = a.Input;
                _userMessages = a.UserMessages;
                _cancellation = a.Cancellation;
                _acceptInfo = a._acceptInfo;
                _output = output;
            }

            internal BasePocoMixer SetCurrentMixer( BasePocoMixer mixer )
            {
                var previous = _mixer;
                _mixer = mixer;
                return previous;
            }

            /// <summary>
            /// Gets the input that must be processed.
            /// </summary>
            public IPoco Input => _input;

            /// <summary>
            /// Gets the optional user message collector.
            /// </summary>
            public UserMessageCollector? UserMessages => _userMessages;

            /// <summary>
            /// Gets the cancellation token.
            /// </summary>
            public CancellationToken Cancellation => _cancellation;

            /// <summary>
            /// Gets the state that <see cref="BasePocoMixer.Accept(AcceptContext, object?)"/>
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
                AcceptContext.EmitError( monitor, _userMessages, _input, _mixer, error );
                // When error is null, we ensure that at least one error user message has been emitted.
                if( error == null && !_hasError && _userMessages != null )
                {
                    _userMessages.Error( $"Mixer '{_mixer.Configuration.Name}' encountered an error." );
                }
                _hasError = true;
            }

            /// <summary>
            /// Outputs a result that may be an intermediate result that requires
            /// a subsequent processing.
            /// </summary>
            /// <param name="output">The result.</param>
            public void Output( IPoco output )
            {
                Throw.CheckNotNullArgument( output );
                _output( output );
            }

        }
    }

}
