using CK.Core;
using CK.Object.Predicate;
using CK.Object.Processor;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace CK.Object.Mixer
{

    public partial class ObjectMixer
    {
        sealed class Result : IObjectMixerResult<object>
        {
            readonly object _input;
            readonly Array _output;
            readonly ImmutableArray<(object,string)> _rejected;
            readonly Exception? _exception;
            readonly int _totalProcessCount;

            internal Result( object input, Exception ex, int totalProcessCount, Type outputType )
            {
                _input = input;
                _exception = ex;
                _totalProcessCount = totalProcessCount;
                _output = Array.CreateInstance( outputType, 0 );
            }

            internal Result( object input,
                             ImmutableArray<(object, string)> rejected,
                             int totalProcessCount,
                             Type outputType,
                             List<object> output )
            {
                _input = input;
                _rejected = rejected;
                _totalProcessCount = totalProcessCount;
                _output = Array.CreateInstance( outputType, output.Count );
                for( int i = 0; i < output.Count; i++ )
                {
                    _output.SetValue( output[i], i );
                }
            }

            public Exception? Exception => _exception;

            public object Input => _input;

            public IReadOnlyList<object> Output => (IReadOnlyList<object>)_output;

            public ImmutableArray<(object Object, string Reason)> Rejected => _rejected;

            public int TotalProcessCount => _totalProcessCount;
        }

        // Don't use a struct here.
        sealed class Mixer
        {
            readonly IActivityMonitor _monitor;
            readonly ObjectMixer _mixer;
            readonly object _input;
            readonly Queue<(int,object)> _queue;
            readonly List<object> _output;
            ImmutableArray<(object,string)>.Builder? _rejected;
            readonly int _maxProcessCount;
            readonly Type _outputType;
            int _totalProcessCount;

            public Mixer( IActivityMonitor monitor, ObjectMixer mixer, object input, int maxProcessCount, Type outputType )
            {
                _monitor = monitor;
                _mixer = mixer;
                _input = input;
                _maxProcessCount = maxProcessCount;
                _outputType = outputType;
                _output = new List<object>();
                _queue = new Queue<(int,object)>();
                _queue.Enqueue( (0, input) );
            }

            public async Task<IObjectMixerResult<object>> GetResultAsync()
            {
                while( _queue.TryDequeue( out var pQueued ) )
                {
                    ++_totalProcessCount;
                    var (processCount, queued) = pQueued;
                    object? processed;
                    try
                    {
                        processed = await _mixer.ProcessAsync( _monitor, queued );
                    }
                    catch( Exception ex )
                    {
                        using( _monitor.OpenError( $"While processing '{queued}' (ProcessCount: {processCount}).", ex ) )
                        {
                            return await _mixer.OnProcessOrConditionErrorAsync( _monitor, _input, ex, queued, _totalProcessCount );
                        }
                    }
                    if( processed == null )
                    {
                        Reject( queued, "Process returned null." );
                    }
                    else if( processed is IReadOnlyCollection<object> multiple )
                    {
                        if( multiple.Count == 0 )
                        {
                            Reject( queued, "Process returned empty IReadOnlyCollection<object>." );
                        }
                        else
                        {
                            foreach( var output in multiple )
                            {
                                var errorResult = await HandleOutputAsync( processCount, queued, output );
                                if( errorResult != null ) return errorResult;
                            }
                        }
                    }
                    else
                    {
                        var errorResult = await HandleOutputAsync( processCount, queued, processed );
                        if( errorResult != null ) return errorResult;
                    }
                }

                return new Result( _input,
                                   _rejected?.ToImmutable() ?? ImmutableArray<(object, string)>.Empty,
                                   _totalProcessCount,
                                   _outputType,
                                   _output );
            }

            void Reject( object queued, string reason )
            {
                _rejected ??= ImmutableArray.CreateBuilder<(object, string)>();
                _rejected.Add( (queued, reason) );
            }

            async ValueTask<IObjectMixerResult<object>?> HandleOutputAsync( int processCount, object queued, object output )
            {
                bool result = _outputType.IsAssignableFrom( output.GetType() );
                if( result )
                {
                    try
                    {
                        result = await _mixer.FilterOutputAsync( _monitor, output );
                    }
                    catch( Exception ex )
                    {
                        using( _monitor.OpenError( $"While filtering '{output}' (ProcessCount: {processCount}).).", ex ) )
                        {
                            return await _mixer.OnProcessOrConditionErrorAsync( _monitor, _input, ex, output, _totalProcessCount );
                        }
                    }
                }
                if( result )
                {
                    _output.Add( output );
                    return null;
                }
                if( ++processCount > _maxProcessCount )
                {
                    Reject( output, $"Reached MaxProcessCount: {_maxProcessCount}." );
                    return null;
                }
                _queue.Enqueue( (processCount, output) );
                return null;
            }
        }

        async Task<IObjectMixerResult<object>> OnProcessOrConditionErrorAsync( IActivityMonitor monitor, object input, Exception ex, object culprit, int totalProcessCount )
        {
            if( _errorInConfiguredProcessor )
            {
                Throw.DebugAssert( _configuration.Processor != null );
                var context = new MonitoredProcessorDescriptorContext( monitor );
                var descriptor = _configuration.Processor.CreateDescriptor( context, _services );
                if( descriptor == null )
                {
                    monitor.Error( ActivityMonitor.Tags.ToBeInvestigated,
                                   $"Unable to obtain a descriptor for configured processor that HAS generated a processor function. " +
                                   $"(Configuration '{_configuration.Processor.ConfigurationPath}')" );
                }
                else
                {
                    // This never throws: the descriptor handles exceptions (and returns the exception object
                    // but we don"t care here: we only want to reproduce the execution with detailed logs).
                    try
                    {
                        await descriptor.ProcessAsync( culprit );
                    }
                    catch( Exception exP )
                    {
                        monitor.Error( ActivityMonitor.Tags.ToBeInvestigated,
                                       $"Descriptor process function threw an exception with a ProcessorDescriptorContext that MUST handle exceptions. " +
                                       $"(Configuration '{_configuration.Processor.ConfigurationPath}')", exP );
                    }
                }
            }
            if( _errorInConfiguredCondition )
            {
                Throw.DebugAssert( _configuration.OutputCondition != null );
                var context = new MonitoredPredicateDescriptorContext( monitor );
                var descriptor = _configuration.OutputCondition.CreateDescriptor( context, _services );
                if( descriptor == null )
                {
                    monitor.Error( ActivityMonitor.Tags.ToBeInvestigated,
                                   $"Unable to obtain a descriptor for configured predicate that HAS generated a predicate function. " +
                                   $"(Configuration '{_configuration.OutputCondition.ConfigurationPath}')" );
                }
                else
                {
                    // This never throws: the descriptor handles exceptions (and returns false).
                    try
                    {
                        await descriptor.EvaluateAsync( culprit );
                    }
                    catch( Exception exC )
                    {
                        monitor.Error( ActivityMonitor.Tags.ToBeInvestigated,
                                       $"Descriptor evaluation function threw an exception with a PredicateDescriptorContext that MUST handle exceptions. " +
                                       $"(Configuration '{_configuration.OutputCondition.ConfigurationPath}')", exC );
                    }
                }
            }
            return new Result( input, ex, totalProcessCount, _configuration.OutputType );
        }

    }
}
