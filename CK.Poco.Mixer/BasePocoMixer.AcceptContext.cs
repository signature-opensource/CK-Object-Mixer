using CK.Core;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using System;

namespace CK.Poco.Mixer
{
    public abstract partial class BasePocoMixer
    {
        /// <summary>
        /// Context provided to <see cref="AcceptAsync(IActivityMonitor, AcceptContext)"/>.
        /// </summary>
        public sealed class AcceptContext
        {
            [AllowNull] IPoco _input;
            // Monitor is not exposed. It only simplifies the API of Accept/Reject/SetError/SetWarning.
            readonly IActivityMonitor _monitor;
            readonly UserMessageCollector? _userMessages;
            readonly string _inputTypeName;
            readonly CancellationToken _cancellation;
            internal BasePocoMixer? _winner;
            internal BasePocoMixer? _culprit;
            internal object? _acceptInfo;
            RejectReason _rejectReason;

            internal AcceptContext( IActivityMonitor monitor,
                                    UserMessageCollector? userMessages,
                                    string inputTypeName,
                                    CancellationToken cancellation )
            {
                _monitor = monitor;
                _userMessages = userMessages;
                _inputTypeName = inputTypeName;
                _cancellation = cancellation;
            }

            internal void Initialize( IPoco input ) => _input = input;

            internal void Accept( BasePocoMixer mixer, object? acceptInfo )
            {
                _userMessages?.Info( $"Accepted by '{mixer.Configuration.Name}'." );
                _culprit = null;
                _winner = mixer;
                _acceptInfo = acceptInfo;
                _rejectReason = RejectReason.None;
            }

            internal void Reject( BasePocoMixer mixer, RejectReason reason )
            {
                if( _rejectReason == reason ) return;
                if( _userMessages != null )
                {
                    if( IsAccepted )
                    {
                        if( reason == RejectReason.None )
                        {
                            _userMessages.Info( $"Previous decision canceled by '{mixer.Configuration.Name}'." );
                        }
                        else
                        {
                            _userMessages.Info( $"Previous decision rejected by '{mixer.Configuration.Name}' with reason '{reason}'." );
                        }
                    }
                    else
                    {
                        _userMessages.Info( $"Rejected by '{mixer.Configuration.Name}' with reason '{reason}'." );
                    }
                }
                _culprit = mixer;
                _winner = null;
                _acceptInfo = null;
                _rejectReason = reason;
            }

            internal void Reject( BasePocoMixer mixer, Exception ex )
            {
                // This will add a user message with the status change.
                Reject( mixer, RejectReason.Error );
                // This will log the exception and append it to the user messages.
                EmitError( _monitor, _userMessages, _input, mixer, ex );
            }

            /// <summary>
            /// This always log the input, even if exception is null.
            /// If exception and userMessages are not null, the exception is appended to the user messages.
            /// </summary>
            internal static void EmitError( IActivityMonitor monitor, UserMessageCollector? userMessages, IPoco input, BasePocoMixer mixer, Exception? ex )
            {
                using( monitor.OpenError( $"Mixer '{mixer.Configuration.Name}' error while processing '{((IPocoGeneratedClass)input).Factory.Name}'.", ex ) )
                {
                    monitor.Trace( input.ToString()! );
                }
                if( ex != null )
                {
                    userMessages?.AppendErrors( ex );
                }
            }

            /// <summary>
            /// Gets the input to accept or reject.
            /// </summary>
            public IPoco Input => _input;

            /// <summary>
            /// Gets the name that describes the type of the input: the target type's <see cref="Type.FullName"/>.
            /// </summary>
            public string InputTypeName => _inputTypeName; 

            /// <summary>
            /// Gets the optional user message collector.
            /// </summary>
            public UserMessageCollector? UserMessages => _userMessages;

            /// <summary>
            /// Gets the cancellation token..
            /// </summary>
            public CancellationToken Cancellation => _cancellation;

            /// <summary>
            /// Gets whether the input has been successfully accepted: the <see cref="Winner"/> is not null.
            /// </summary>
            [MemberNotNullWhen( true, nameof(Winner) )]
            public bool IsAcceptedSuccessfully => _winner != null;

            /// <summary>
            /// Gets the non null winner if <see cref="IsAcceptedSuccessfully"/> is true.
            /// </summary>
            public BasePocoMixer? Winner => _winner;

            /// <summary>
            /// Gets whether the input has been accepted, either successfuly or with an error
            /// (reason is not <see cref="RejectReason.None"/>).
            /// </summary>
            public bool IsAccepted => _winner != null || _rejectReason > RejectReason.None;

            /// <summary>
            /// Gets the rejection reason.
            /// </summary>
            public RejectReason RejectReason => _rejectReason;
        }
    }

}
