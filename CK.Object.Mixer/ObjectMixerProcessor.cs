using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Object.Mixer
{
    public sealed class ObjectMixerProcessor
    {
        readonly BaseObjectMixer _mixer;
        readonly ObjectMixerFactory _driver;
        readonly UserMessageCollector? _userMessages;
        readonly CancellationToken _cancellation;
        readonly List<object> _outputs;
        readonly Queue<object> _remainder;
        List<object>? _rejected;
        bool _error;



        public ObjectMixerProcessor( BaseObjectMixer mixer,
                                     UserMessageCollector? userMessages,
                                     ObjectMixerFactory driver,
                                     CancellationToken cancellation )
        {
            _mixer = mixer;
            _userMessages = userMessages;
            _cancellation = cancellation;
            _driver = driver;
            _outputs = new List<object>();
            _remainder = new Queue<object>();
        }

        /// <summary>
        /// Opaque result. Use <see cref="ObjectMixerResult{T}"/> to expose the result.
        /// </summary>
        public readonly struct UntypedMixerResult
        {
            internal readonly IReadOnlyList<object> _outputs;
            internal readonly IReadOnlyList<UserMessage> _userMessages;
            internal readonly IReadOnlyList<object> _rejected;
            internal readonly bool _success;

            internal UntypedMixerResult( bool success,
                                         IReadOnlyList<object>? outputs = null,
                                         IReadOnlyList<object>? rejected = null,
                                         UserMessageCollector? userMessages = null )
            {
                _success = success;
                _outputs = outputs ?? Array.Empty<object>();
                _userMessages = (IReadOnlyList<UserMessage>?)userMessages?.UserMessages ?? Array.Empty<UserMessage>();
                _rejected = rejected ?? Array.Empty<object>();
            }
        }

        public async ValueTask<UntypedMixerResult> ProcessAsync( IActivityMonitor monitor, object input )
        {
            try
            {
                _remainder.Enqueue( input );
                await DoProcessAsync( monitor );
            }
            catch( Exception ex )
            {
                _error = true;
                BaseObjectMixer.AcceptContext.EmitError( monitor, _driver, _userMessages, input, _mixer, ex );
            }
            return new UntypedMixerResult( _error, _outputs, _rejected, _userMessages );
        }

        async ValueTask DoProcessAsync( IActivityMonitor monitor )
        {
            int inputNumber = 0;
            var aC = new BaseObjectMixer.AcceptContext( monitor, _userMessages, _driver, _cancellation );
            var pC = new BaseObjectMixer.ProcessContext();
            while( _remainder.TryDequeue( out var input ) )
            {
                aC.Initialize( input );
                await _mixer.AcceptAsync( monitor, aC );
                if( !aC.IsAcceptedSuccessfully )
                {
                    if( aC.RejectReason == RejectReason.IgnoreInput )
                    {
                        Throw.DebugAssert( aC._culprit != null );
                        monitor.Trace( $"Input '{_driver.GetDisplayName( input )}' has been ignored by '{_driver.GetMixerName( aC._culprit.Configuration )}'." );
                    }
                    else
                    {
                        if( aC.RejectReason == RejectReason.None )
                        {
                            _userMessages?.Error( $"No mixer accepted the {_driver.InputTypeName}." );
                            monitor.Error( $"No mixer found for '{_driver.GetDisplayName( input )}' in '{_driver.GetMixerName( _mixer.Configuration )}'." );
                        }
                        else
                        {
                            Throw.DebugAssert( aC._culprit != null );
                            monitor.Error( $"Input '{_driver.GetDisplayName( input )}' has been rejected with reason '{aC.RejectReason}' by '{_driver.GetMixerName( aC._culprit.Configuration )}'." );
                        }
                        _rejected ??= new List<object>();
                        _rejected.Add( input );
                        if( FailOnFirstError() ) break;
                    }
                }
                else
                {
                    pC.Initialize( aC, Receive );
                    await aC.Winner.ProcessAsync( monitor, pC );
                    if( pC.HasError && FailOnFirstError() ) break;
                }
                if( ++inputNumber > _driver.MaxMixCount && _remainder.Count > 0 )
                {
                    _error = true;
                    _userMessages?.Error( $"Mixing {_driver.InputTypeName} required more than maximun {_driver.MaxMixCount} steps." );
                    using( monitor.OpenError( $"Mixer '{_driver.GetMixerName( _mixer.Configuration )}' reached its maximum {_driver.MaxMixCount} steps." ) )
                    {
                        var remaiders = _remainder.Select( _driver.GetDisplayName ).Concatenate();
                        monitor.Trace( $"Remainders: {remaiders}." );
                    }
                    break;
                }
            }
        }

        bool FailOnFirstError()
        {
            _error = true;
            return _driver.FailOnFirstError;
        }

        void Receive( BaseObjectMixer.ProcessContext context, object output )
        {
            if( _driver.IsOutput( output ) ) _outputs.Add( output );
            else _remainder.Enqueue( output );
        }
    }
}
