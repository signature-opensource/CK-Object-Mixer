using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Processor;

/// <summary>
/// Descriptor of <see cref="ObjectProcessorConfiguration"/>.
/// </summary>
public class ObjectProcessorDescriptor
{
    readonly ProcessorDescriptorContext _context;
    readonly ObjectProcessorConfiguration _configuration;
    readonly ObjectPredicateDescriptor? _condition;
    readonly ImmutableArray<ObjectProcessorDescriptor> _processors;
    readonly ObjectTransformDescriptor? _transform;
    readonly bool _isSync;

    /// <summary>
    /// Initializes a new descriptor.
    /// </summary>
    /// <param name="context">The descriptor context.</param>
    /// <param name="configuration">The processor configuration.</param>
    /// <param name="condition">The condition descriptor.</param>
    /// <param name="processors">The processor descriptors.</param>
    /// <param name="transform">The transform descriptor.</param>
    public ObjectProcessorDescriptor( ProcessorDescriptorContext context,
                                      ObjectProcessorConfiguration configuration,
                                      ObjectPredicateDescriptor? condition,
                                      ImmutableArray<ObjectProcessorDescriptor> processors,
                                      ObjectTransformDescriptor? transform )
    {
        Throw.CheckNotNullArgument( context );
        Throw.CheckNotNullArgument( configuration );
        _context = context;
        _configuration = configuration;
        _condition = condition;
        _processors = processors;
        _transform = transform;
        _isSync = (condition is null || condition.IsSynchronous)
                  && (transform is null || transform.IsSynchronous)
                  && processors.All( p => p._isSync );
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public ObjectProcessorConfiguration Configuration => _configuration;

    /// <summary>
    /// Gets the descriptor context to which this descriptor is bound.
    /// </summary>
    public ProcessorDescriptorContext Context => _context;

    /// <summary>
    /// Gets the optional condition descriptor.
    /// </summary>
    public ObjectPredicateDescriptor? Condition => _condition;

    /// <summary>
    /// Gets the optional transform descriptor.
    /// </summary>
    public ObjectTransformDescriptor? Transform => _transform;

    /// <summary>
    /// Gets the subordinated processor descriptors.
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
        if( _condition != null && !_condition.EvaluateSync( o ) )
        {
            return null;
        }
        var r = _processors.Length == 0 ? o : ApplyInner( o );
        if( r == null ) return null;
        return _transform != null ? _transform.TransformSync( r ) : r;
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
