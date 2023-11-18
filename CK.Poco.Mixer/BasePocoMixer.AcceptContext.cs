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
            readonly UserMessageCollector? _userMessages;
            readonly string _inputTypeName;
            readonly CancellationToken _cancellation;
            internal BasePocoMixer? _winner;
            internal BasePocoMixer? _culprit;
            internal object? _acceptInfo;
            RejectReason _rejectReason;

            internal AcceptContext( UserMessageCollector? userMessages,
                                    string inputTypeName,
                                    CancellationToken cancellation )
            {
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
