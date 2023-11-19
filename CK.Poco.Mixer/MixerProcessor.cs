using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static CK.Core.CompletionSource;

namespace CK.Poco.Mixer
{
    sealed class MixerProcessor
    {
        readonly BasePocoMixer _mixer;
        readonly Type _targetType;
        readonly UserMessageCollector? _userMessages;
        readonly string _inputTypeName;
        readonly CancellationToken _cancellation;
        readonly int _maxMixCount;
        readonly List<IPoco> _outputs;
        readonly Queue<IPoco> _remainder;
        List<IPoco>? _rejected;
        bool _error;
        bool _failOnFirstError;

        public MixerProcessor( BasePocoMixer mixer,
                          Type targetType,
                          UserMessageCollector? userMessages,
                          int maxMixCount,
                          bool failOnFirstError,
                          CancellationToken cancellation )
        {
            _mixer = mixer;
            _targetType = targetType;
            _userMessages = userMessages;
            _inputTypeName = targetType.FullName ?? "input";
            _cancellation = cancellation;
            _maxMixCount = maxMixCount;
            _failOnFirstError = failOnFirstError;
            _outputs = new List<IPoco>();
            _remainder = new Queue<IPoco>();
        }

        public readonly struct UntypedMixerResult
        {
            internal readonly IReadOnlyList<IPoco> _outputs;
            internal readonly IReadOnlyList<UserMessage> _messages;
            internal readonly IReadOnlyList<IPoco> _rejected;
            internal readonly bool _success;

            internal UntypedMixerResult( bool success, IReadOnlyList<IPoco>? outputs = null, IReadOnlyList<IPoco>? rejected = null, UserMessageCollector? messages = null )
            {
                _success = success;
                _outputs = outputs ?? Array.Empty<IPoco>();
                _messages = (IReadOnlyList<UserMessage>?)messages?.UserMessages ?? Array.Empty<UserMessage>();
                _rejected = rejected ?? Array.Empty<IPoco>();
            }
        }

        public async ValueTask<UntypedMixerResult> ProcessAsync( IActivityMonitor monitor, IPoco input )
        {
            try
            {
                _remainder.Enqueue( input );
                await DoProcessAsync( monitor );
            }
            catch( Exception ex )
            {
                _error = true;
                BasePocoMixer.AcceptContext.EmitError( monitor, _userMessages, input, _mixer, ex );
            }
            return new UntypedMixerResult( _error, _outputs, _rejected, _userMessages );
        }

        async ValueTask DoProcessAsync( IActivityMonitor monitor )
        {
            int inputNumber = 0;
            var aC = new BasePocoMixer.AcceptContext( monitor, _userMessages, _inputTypeName, _cancellation );
            var pC = new BasePocoMixer.ProcessContext();
            while( _remainder.TryDequeue( out var input ) )
            {
                aC.Initialize( input );
                await _mixer.AcceptAsync( monitor, aC );
                if( !aC.IsAcceptedSuccessfully )
                {
                    if( aC.RejectReason == RejectReason.IgnoreInput )
                    {
                        Throw.DebugAssert( aC._culprit != null );
                        monitor.Trace( $"Input '{((IPocoGeneratedClass)input).Factory.Name}' has been ignored by '{aC._culprit.Configuration.Name}'." );
                    }
                    else
                    {
                        if( aC.RejectReason == RejectReason.None )
                        {
                            _userMessages?.Error( $"No mixer accepted the {_inputTypeName}." );
                            monitor.Error( $"No mixer found for '{((IPocoGeneratedClass)input).Factory.Name}' in '{_mixer.Configuration.Name}'." );
                        }
                        else
                        {
                            Throw.DebugAssert( aC._culprit != null );
                            monitor.Error( $"Input '{((IPocoGeneratedClass)input).Factory.Name}' has been rejected with reason '{aC.RejectReason}' by '{aC._culprit.Configuration.Name}'." );
                        }
                        _rejected ??= new List<IPoco>();
                        _rejected.Add( input );
                        if( FailOnFirstError() ) break;
                    }
                }
                else
                {
                    pC.Initialize( aC, Receive );
                    await aC.Winner.DoProcessAsync( monitor, pC );
                    if( pC.HasError && FailOnFirstError() ) break;
                }
                if( ++inputNumber > _maxMixCount && _remainder.Count > 0 )
                {
                    _error = true;
                    _userMessages?.Error( $"Mixing {_inputTypeName} required more than maximun {_maxMixCount} steps." );
                    using( monitor.OpenError( $"Mixer '{_mixer.Configuration.Name}' reached its maximum {_maxMixCount} steps." ) )
                    {
                        var remaiders = _remainder.Select( r => ((IPocoGeneratedClass)r).Factory.Name ).Concatenate();
                        monitor.Trace( $"Remainders: {remaiders}." );
                    }
                    break;
                }
            }
        }

        bool FailOnFirstError()
        {
            _error = true;
            return _failOnFirstError;
        }

        void Receive( IPoco output )
        {
            if( _targetType.IsAssignableFrom( output.GetType() ) ) _outputs.Add( output );
            else _remainder.Enqueue( output );
        }
    }
}
