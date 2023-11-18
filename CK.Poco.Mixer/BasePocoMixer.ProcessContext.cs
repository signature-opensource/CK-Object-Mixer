using CK.Core;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Poco.Mixer
{
    public abstract partial class BasePocoMixer
    {
        /// <summary>
        /// Context provided to <see cref="ProcessAsync(IActivityMonitor, ProcessContext)"/>.
        /// </summary>
        public sealed class ProcessContext
        {
            [AllowNull] IPoco _input;
            [AllowNull] UserMessageCollector? _userMessages;
            [AllowNull] CancellationToken _cancellation;
            [AllowNull] object? _acceptInfo;
            [AllowNull] Action<IPoco> _output;

            internal void Initialize( AcceptContext a, Action<IPoco> output )
            {
                _input = a.Input;
                _userMessages = a.UserMessages;
                _cancellation = a.Cancellation;
                _acceptInfo = a._acceptInfo;
                _output = output;
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
