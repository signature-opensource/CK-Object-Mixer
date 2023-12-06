using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    /// <summary>
    /// Descriptor of <see cref="ObjectProcessorConfiguration"/>.
    /// </summary>
    public class ObjectProcessorDescriptor
    {
        readonly ProcessorDescriptorContext _context;
        readonly ObjectProcessorConfiguration _configuration;
        readonly IObjectPredicateHook? _condition;
        readonly ImmutableArray<ObjectProcessorDescriptor> _processors;
        readonly IObjectTransformHook? _transform;
        readonly bool _isSync;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The processor configuration.</param>
        /// <param name="condition">The condition hook.</param>
        /// <param name="processors">The processor hooks.</param>
        /// <param name="transform">The transform hook.</param>
        public ObjectProcessorDescriptor( ProcessorDescriptorContext context,
                                          ObjectProcessorConfiguration configuration,
                                          IObjectPredicateHook? condition,
                                          ImmutableArray<ObjectProcessorDescriptor> processors,
                                          IObjectTransformHook? transform )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            _context = context;
            _configuration = configuration;
            _condition = condition;
            _processors = processors;
            _transform = transform;
            _isSync = condition is null or ObjectPredicateHook
                      && transform is null or ObjectTransformHook
                      && processors.All( p => p._isSync );
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public ObjectProcessorConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets the hook context to which this hook is bound.
        /// </summary>
        public ProcessorDescriptorContext Context => _context;

        /// <summary>
        /// Gets the optional condition hook.
        /// </summary>
        public IObjectPredicateHook? Condition => _condition;

        /// <summary>
        /// Gets the optional transform hook.
        /// </summary>
        public IObjectTransformHook? Transform => _transform;

        /// <summary>
        /// Gets the subordinated processor hooks.
        /// </summary>
        public ImmutableArray<ObjectProcessorDescriptor> Processors => _processors;

        /// <summary>
        /// Gets whether <see cref="SyncProcess(object)"/> can be called.
        /// </summary>
        public bool IsSynchronous => _isSync;

        /// <summary>
        /// Asynchronously processes the input object.
        /// </summary>
        /// <param name="o">The object to process.</param>
        /// <returns>The processed object or null if this processor rejects this input.</returns>
        public virtual async ValueTask<object?> ProcessAsync( object o )
        {
            Throw.CheckNotNullArgument( o );
            if( _condition != null && !await _condition.EvaluateAsync( o ).ConfigureAwait( false ) )
            {
                return null;
            }
            var r = _processors.Length == 0 ? o : await ApplyInnerAsync( o ).ConfigureAwait( false );
            if( r == null ) return null;
            return _transform != null ? await _transform.TransformAsync( r ).ConfigureAwait( false ) : r;
        }

        async ValueTask<object?> ApplyInnerAsync( object o )
        {
            foreach( var p in _processors )
            {
                var r = await p.ProcessAsync( o ).ConfigureAwait( false );
                if( r != null ) return r;
            }
            return null;
        }

        /// <summary>
        /// Synchronously processes the input object. <see cref="IsSynchronous"/> must be true otherwise
        /// an <see cref="InvalidOperationException"/> is thrown.
        /// <para>
        /// This is named "SyncProcess" to avoid https://github.com/Microsoft/vs-threading/blob/main/doc/analyzers/VSTHRD103.md.
        /// </para>
        /// </summary>
        /// <param name="o">The object to process.</param>
        /// <returns>The processed object or null if this processor rejects this input.</returns>
        public virtual object? SyncProcess( object o )
        {
            Throw.CheckState( IsSynchronous );
            Throw.CheckNotNullArgument( o );
            if( _condition != null && !Unsafe.As<ObjectPredicateHook>( _condition ).Evaluate( o ) )
            {
                return null;
            }
            var r = _processors.Length == 0 ? o : ApplyInner( o );
            if( r == null ) return null;
            return _transform != null ? Unsafe.As<ObjectTransformHook>( _transform ).Transform( r ) : r;
        }

        object? ApplyInner( object o )
        {
            foreach( var p in _processors )
            {
                var r = p.SyncProcess( o );
                if( r != null ) return r;
            }
            return null;
        }
    }

}
