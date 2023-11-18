using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Poco.Mixer
{
    public sealed class SimplePocoMixer<T> where T : class, IPoco
    {
        readonly IServiceProvider _services;
        readonly PocoMixerConfiguration _configuration;

        public SimplePocoMixer( IServiceProvider services, PocoMixerConfiguration configuration )
        {
            Throw.CheckNotNullArgument( services );
            Throw.CheckNotNullArgument( configuration );
            _services = services;
            _configuration = configuration;
        }

        public sealed class Result
        {
            readonly bool _success;
            readonly IReadOnlyList<T> _outputs;
            readonly ImmutableArray<UserMessage> _messages;
            readonly IReadOnlyList<IPoco> _rejected;

            internal Result( bool success, IReadOnlyList<T>? outputs = null, UserMessageCollector? messages = null )
            {
                _success = success;
                _outputs = outputs ?? ImmutableArray<T>.Empty;
                _messages = messages?.UserMessages.ToImmutableArray() ?? ImmutableArray<UserMessage>.Empty;
            }

            public bool Success => _success;

            public IReadOnlyList<T> Outputs => _outputs;

            public ImmutableArray<UserMessage> Messages => _messages;
        }

        sealed class Processor
        {
            readonly BasePocoMixer _mixer;
            readonly UserMessageCollector? _userMessages;
            readonly string _inputTypeName;
            readonly CancellationToken _cancellation;
            readonly List<T> _outputs;
            readonly Queue<IPoco> _remainder;
            List<IPoco>? _rejected;
            IPoco? _processFailure;

            public Processor( BasePocoMixer mixer, UserMessageCollector? userMessages, string? inputTypeName, CancellationToken cancellation )
            {
                _mixer = mixer;
                _userMessages = userMessages;
                _inputTypeName = inputTypeName ?? typeof(T).FullName ?? "input";
                _cancellation = cancellation;
                _outputs = new List<T>();
                _remainder = new Queue<IPoco>();
            }

            public ValueTask ProcessAsync( IActivityMonitor monitor, IPoco input )
            {
                _remainder.Enqueue( input );
                return DoProcessAsync( monitor );
            }

            async ValueTask DoProcessAsync( IActivityMonitor monitor )
            {
                int inputNumber = 0;
                var aC = new BasePocoMixer.AcceptContext( _userMessages, _inputTypeName, _cancellation );
                var pC = new BasePocoMixer.ProcessContext();
                while( _remainder.TryDequeue( out var input ) )
                {
                    aC.Initialize( input );
                    await _mixer.AcceptAsync( monitor, aC );
                    if( !aC.IsAcceptedSuccessfully )
                    {
                        if( aC.RejectReason != RejectReason.IgnoreInput )
                        {
                            if( aC.RejectReason == RejectReason.None )
                            {
                                _userMessages?.Error( $"No mixer accepted the {_inputTypeName}." );
                            }
                            _rejected ??= new List<IPoco>();
                            _rejected.Add( input );
                        }
                    }
                    else 
                    {
                        pC.Initialize( aC, Receive );
                        await aC.Winner.ProcessAsync( monitor, pC );
                    }
                    ++inputNumber;
                }
            }

            void Receive( IPoco output )
            {
                if( output is T result ) _outputs.Add( result );
                else _remainder.Enqueue( output );
            }
        }

        //public async ValueTask<Result> MixAsync( IActivityMonitor monitor, IPoco input, UserMessageCollector? userMessages )
        //{
        //    var mixer = _configuration.CreateMixer( monitor, _services );
        //    if( mixer == null )
        //    {
        //        monitor.Error( $"Empty mixer configured by '{_configuration.Name}'." );
        //        return new Result( false );
        //    }
        //}

    }
}
