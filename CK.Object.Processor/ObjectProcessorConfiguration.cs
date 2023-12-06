using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    /// <summary>
    /// Configuration class for Processor.
    /// <para>
    /// This always supports asynchronous processors <see cref="CreateAsyncProcessor(IActivityMonitor, IServiceProvider)"/> but
    /// supports synchronous processors (<see cref="CreateProcessor(IActivityMonitor, IServiceProvider)"/> only if no async predicates
    /// or transformation exist: see <see cref="IsSynchronous"/>.
    /// </para>
    /// <para>
    /// This is a concrete type that handles a optional "Condition" (<see cref="ConfiguredCondition"/>), "Tranform" (<see cref="ConfiguredTransform"/>)
    /// and "Processors" (<see cref="Processors"/>).
    /// </para>
    /// </summary>
    public partial class ObjectProcessorConfiguration
    {
        readonly string _configurationPath;
        // Configuration (including subordinated processors).
        // These are not readonly because of the Clone method that uses MemberWiseClone:
        // cloned objects are patched with modified configured condition, transform and processors
        // (and _initialized is set to false).
        ObjectAsyncPredicateConfiguration? _cCondition;
        ObjectAsyncTransformConfiguration? _cTransform;
        ImmutableArray<ObjectProcessorConfiguration> _processors;
        // Intrinsic
        ObjectAsyncPredicateConfiguration? _iCondition;
        ObjectAsyncTransformConfiguration? _iTransform;
        // Final
        ObjectAsyncPredicateConfiguration? _fCondition;
        ObjectAsyncTransformConfiguration? _fTransform;
        PKind _pKind;
        bool _isSyncProcessors;
        bool _initialized;

        enum PKind
        {
            Void,
            SyncFull,
            SyncCondition,
            SyncTransform,
            // 
            AsyncFull,
            SyncCAsyncT,
            AsyncCSyncT,
            AsyncCondition,
            AsyncTransform
        }

        /// <summary>
        /// Handles "Condition", "Transform" and "Processors" from the configuration section.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors and warnings.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration for this object.</param>
        public ObjectProcessorConfiguration( IActivityMonitor monitor,
                                             TypedConfigurationBuilder builder,
                                             ImmutableConfigurationSection configuration,
                                             IReadOnlyList<ObjectProcessorConfiguration> processors )
        {
            _configurationPath = configuration.Path;
            var cCond = configuration.TryGetSection( "Condition" );
            if( cCond != null )
            {
                _cCondition = builder.HasBaseType<ObjectAsyncPredicateConfiguration>()
                                ? builder.Create<ObjectAsyncPredicateConfiguration>( monitor, cCond )
                                : builder.Create<ObjectPredicateConfiguration>( monitor, cCond );
            }
            var cTrans = configuration.TryGetSection( "Transform" );
            if( cTrans != null )
            {
                _cTransform = builder.HasBaseType<ObjectAsyncTransformConfiguration>()
                                ? builder.Create<ObjectAsyncTransformConfiguration>( monitor, cTrans )
                                : builder.Create<ObjectTransformConfiguration>( monitor, cTrans );
            }
            _processors = processors.ToImmutableArray();
        }

        /// <summary>
        /// Optional mutation constructor. This should be used only if the default <see cref="Clone"/>
        /// method is not enough.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="configuredCondition">The configured condition to consider.</param>
        /// <param name="configuredTransform">The configured transform to consider.</param>
        /// <param name="processors">The processors to consider.</param>
        protected ObjectProcessorConfiguration( ObjectProcessorConfiguration source,
                                                ObjectAsyncPredicateConfiguration? configuredCondition,
                                                ObjectAsyncTransformConfiguration? configuredTransform,
                                                ImmutableArray<ObjectProcessorConfiguration> processors )
        {
            Throw.CheckNotNullArgument( source );
            _configurationPath = source._configurationPath;
            _iCondition = source._iCondition;
            _iTransform = source._iTransform;
            _cCondition = configuredCondition;
            _cTransform = configuredTransform;
            _processors = processors;
        }

        /// <summary>
        /// Clones this object by using <see cref="object.MemberwiseClone()"/>.
        /// This should work almost all the time but if more control is required, this method
        /// can be overridden and a mutation constructor must be specifically designed.
        /// </summary>
        /// <param name="configuredCondition">The configured condition to consider.</param>
        /// <param name="configuredTransform">The configured transform to consider.</param>
        /// <param name="processors">The processors to consider.</param>
        /// <returns>A mutated clone of this processor configuration.</returns>
        internal protected virtual ObjectProcessorConfiguration Clone( ObjectAsyncPredicateConfiguration? configuredCondition,
                                                                       ObjectAsyncTransformConfiguration? configuredTransform,
                                                                       ImmutableArray<ObjectProcessorConfiguration> processors )
        {
            var c = (ObjectProcessorConfiguration)MemberwiseClone();
            c._initialized = false;
            c._cCondition = configuredCondition;
            c._cTransform = configuredTransform;
            c._processors = processors;
            return c;
        }

        /// <summary>
        /// Gets the configuration path.
        /// </summary>
        public string ConfigurationPath => _configurationPath;

        /// <summary>
        /// Gets the optional configured condition.
        /// </summary>
        public ObjectAsyncPredicateConfiguration? ConfiguredCondition => _cCondition;

        /// <summary>
        /// Gets the optional configured transformation.
        /// </summary>
        public ObjectAsyncTransformConfiguration? ConfiguredTransform => _cTransform;

        /// <summary>
        /// Gets the optional subordinated processors.
        /// </summary>
        public ImmutableArray<ObjectProcessorConfiguration> Processors => _processors;

        /// <summary>
        /// Gets whether this processor can create synchronous processors.
        /// <para>
        /// This is true if and only if no asynchronous predicates or transform appear in this processor.
        /// </para>
        /// </summary>
        public bool IsSynchronous => Initialize() <= PKind.AsyncFull && _isSyncProcessors;

        PKind Initialize()
        {
            if( !_initialized )
            {
                _initialized = true;
                var fC = _fCondition = ObjectAsyncPredicateConfiguration.Combine( ConfigurationPath, IntrinsicCondition, _cCondition );
                var fT = _fTransform = ObjectAsyncTransformConfiguration.Combine( ConfigurationPath, IntrinsicTransform, _cTransform );
                _isSyncProcessors = _processors.All( p => p.IsSynchronous );
                if( fC != null )
                {
                    var cSync = fC.Synchronous;
                    if( fT != null )
                    {
                        var tSync = fT.Synchronous;
                        if( cSync != null )
                        {
                            if( tSync != null )
                            {
                                return _pKind = PKind.SyncFull;
                            }
                            return _pKind = PKind.SyncCAsyncT;
                        }
                        if( tSync != null )
                        {
                            return _pKind = PKind.AsyncCSyncT;
                        }
                        return _pKind = PKind.AsyncFull;
                    }
                    if( cSync != null )
                    {
                        return _pKind = PKind.SyncCondition;
                    }
                    return _pKind = PKind.AsyncCondition;
                }
                else if( fT != null )
                {
                    var tSync = fT.Synchronous;
                    if( tSync != null )
                    {
                        return _pKind = PKind.SyncTransform;
                    }
                    return _pKind = PKind.AsyncTransform;
                }
            }
            return _pKind;
        }

        /// <summary>
        /// Creates a synchronous processor function. Must be called only if <see cref="IsSynchronous"/>
        /// is true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured processor function or null for a void processor.</returns>
        public virtual Func<object, object?>? CreateProcessor( IActivityMonitor monitor, IServiceProvider services )
        {
            Throw.CheckState( IsSynchronous );
            return CreateProcessor( monitor, services, _fCondition?.Synchronous, _fTransform?.Synchronous, _processors );
        }

        object? CreateHybrid( IActivityMonitor monitor, IServiceProvider services )
        {
            var k = Initialize();
            if( k < PKind.AsyncFull && _isSyncProcessors )
            {
                return CreateProcessor( monitor, services, _fCondition?.Synchronous, _fTransform?.Synchronous, _processors );
            }
            return k switch
            {
                PKind.SyncCAsyncT => CreateHybridAsyncProcessor( monitor, services, _fCondition!.Synchronous!, _fTransform!, _processors ),
                PKind.AsyncCSyncT => CreateHybridAsyncProcessor( monitor, services, _fCondition!, _fTransform!.Synchronous!, _processors ),
                _ => CreateAsyncProcessor( monitor, services, _fCondition, _fTransform, _processors )
            };
        }

        /// <summary>
        /// Creates an asynchronous processor function.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="services">Services that may be required for some (complex) processors.</param>
        /// <returns>A configured processor function or null for a void processor.</returns>
        public virtual Func<object, ValueTask<object?>>? CreateAsyncProcessor( IActivityMonitor monitor, IServiceProvider services )
        {
            var o = CreateHybrid( monitor, services );
            if( o is Func<object,object?> sync )
            {
                return o => ValueTask.FromResult( sync( o ) );
            }
            return Unsafe.As<Func<object, ValueTask<object?>>?>( o );
        }

        static Func<object, ValueTask<object?>>? CreateAsyncProcessor( IActivityMonitor monitor,
                                                                       IServiceProvider services,
                                                                       ObjectAsyncPredicateConfiguration? condition,
                                                                       ObjectAsyncTransformConfiguration? transform,
                                                                       ImmutableArray<ObjectProcessorConfiguration> processors )
        {
            Func<object, ValueTask<bool>>? c = condition?.CreateAsyncPredicate( services );
            Func<object, ValueTask<object>>? t = transform?.CreateAsyncTransform( services );
            object? inner = CreateHybridInnerProcessor( monitor, services, processors );
            if( c != null )
            {
                if( t != null )
                {
                    if( inner != null )
                    {
                        object? r;
                        if( inner is Func<object, object?> syncInner )
                        {
                            return async o => await c( o ).ConfigureAwait( false )
                                                ? ((r = syncInner( o )) != null
                                                        ? await t( r ).ConfigureAwait( false )
                                                        : null)
                                                : null;
                        }
                        return async o => await c( o ).ConfigureAwait( false )
                                            ? ((r = await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )) != null
                                                    ? await t( r ).ConfigureAwait( false )
                                                    : null)
                                            : null;
                    }
                    // "Full processor" with its conditonned action.
                    return async o => await c( o ).ConfigureAwait( false )
                                        ? await t( o ).ConfigureAwait( false )
                                        : null;
                }
                if( inner != null )
                {
                    if( inner is Func<object, object?> syncInner )
                    {
                        return async o => await c( o ).ConfigureAwait( false ) ? syncInner( o ) : null;
                    }
                    return async o => await c( o ).ConfigureAwait( false )
                                        ? await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )
                                        : null;
                }
                // Condition only processor: the action is the identity function.
                return async o => await c( o ).ConfigureAwait( false ) ? o : null;
            }
            // No condition...
            if( t != null )
            {
                if( inner != null )
                {
                    object? r;
                    if( inner is Func<object, object?> syncInner )
                    {
                        return async o => (r = syncInner( o )) != null
                                            ? await t( r ).ConfigureAwait( false )
                                            : null;
                    }
                    return async o => (r = await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )) != null
                                        ? await t( r ).ConfigureAwait( false )
                                        : null;
                }
                // The action is the process
                // (it returns a non null object, that is compatible with a object?: use the bang operator).
                return t!;
            }
            // No condition, no action: the inner processor or a void processor.
            if( inner is Func<object, object?> sync )
            {
                return o => ValueTask.FromResult( sync( o ) );
            }
            return Unsafe.As<Func<object, ValueTask<object?>>?>( inner );
        }

        static Func<object, ValueTask<object?>>? CreateHybridAsyncProcessor( IActivityMonitor monitor,
                                                                             IServiceProvider services,
                                                                             ObjectAsyncPredicateConfiguration condition,
                                                                             ObjectTransformConfiguration transform,
                                                                             ImmutableArray<ObjectProcessorConfiguration> processors )
        {
            Func<object, ValueTask<bool>>? c = condition.CreateAsyncPredicate( services );
            Func<object, object>? t = transform.CreateTransform( services );
            object? inner = CreateHybridInnerProcessor( monitor, services, processors );
            if( c != null )
            {
                if( t != null )
                {
                    if( inner != null )
                    {
                        object? r;
                        if( inner is Func<object,object?> syncInner )
                        {
                            return async o => await c( o ).ConfigureAwait( false )
                                    ? ((r = syncInner( o )) != null ? t( r ) : null)
                                    : null;
                        }
                        return async o => await c( o ).ConfigureAwait( false )
                                ? ((r = await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )) != null
                                    ? t( r )
                                    : null)
                                : null;
                    }
                    // "Full processor" with its conditonned action.
                    return async o => await c( o ).ConfigureAwait( false ) ? t( o ) : null;
                }
                if( inner != null )
                {
                    if( inner is Func<object, object?> syncInner )
                    {
                        return async o => await c( o ).ConfigureAwait( false ) ? syncInner( o ) : null;
                    }
                    return async o => await c( o ).ConfigureAwait( false )
                            ? await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )
                            : null;
                }
                // Condition only processor: the action is the identity function.
                return async o => await c( o ).ConfigureAwait( false ) ? o : null;
            }
            // No condition...
            if( t != null )
            {
                if( inner != null )
                {
                    object? r;
                    if( inner is Func<object, object?> syncInner )
                    {
                        return o => ValueTask.FromResult( (r = syncInner(o)) != null ? t( r ) : null);
                    }
                    return async o => (r = await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )) != null
                                         ? t( r )
                                         : null;
                }
                // The action is the process
                // (it returns a non null object, that is compatible with a object?: use the bang operator).
                return o => ValueTask.FromResult( t( o ) )!;
            }
            // No condition, no action: the inner processor or a void processor.
            if( inner is Func<object, object?> sync )
            {
                return o => ValueTask.FromResult( sync( o ) );
            }
            return Unsafe.As<Func<object, ValueTask<object?>>?>( inner );
        }

        static Func<object, ValueTask<object?>>? CreateHybridAsyncProcessor( IActivityMonitor monitor,
                                                                             IServiceProvider services,
                                                                             ObjectPredicateConfiguration condition,
                                                                             ObjectAsyncTransformConfiguration transform,
                                                                             ImmutableArray<ObjectProcessorConfiguration> processors )
        {
            Func<object, bool>? c = condition.CreatePredicate( services );
            Func<object, ValueTask<object>>? t = transform.CreateAsyncTransform( services );
            object? inner = CreateHybridInnerProcessor( monitor, services, processors );
            if( c != null )
            {
                if( t != null )
                {
                    if( inner != null )
                    {
                        object? r;
                        if( inner is Func<object, object?> syncInner )
                        {
                            return async o => c( o )
                                    ? ((r = syncInner( o )) != null ? await t( r ).ConfigureAwait( false ) : null)
                                    : null;
                        }
                        return async o => c( o )
                                ? ((r = await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )) != null
                                      ? await t( r ).ConfigureAwait( false )
                                      : null)
                                : null;
                    }
                    // "Full processor" with its conditonned action.
                    return async o => c( o ) ? await t( o ).ConfigureAwait( false ) : null;
                }
                if( inner != null )
                {
                    if( inner is Func<object, object?> syncInner )
                    {
                        return o => ValueTask.FromResult( c( o ) ? syncInner( o ) : null );
                    }
                    return async o => c( o )
                                        ? await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )
                                        : null;
                }
                // Condition only processor: the action is the identity function.
                return o => ValueTask.FromResult( c( o ) ? o : null );
            }
            // No condition...
            if( t != null )
            {
                if( inner != null )
                {
                    object? r;
                    if( inner is Func<object, object?> syncInner )
                    {
                        return async o => (r = syncInner( o )) != null
                                            ? await t( r ).ConfigureAwait( false )
                                            : null;
                    }
                    return async o => (r = await Unsafe.As<Func<object, ValueTask<object?>>>( inner )( o ).ConfigureAwait( false )) != null
                                        ? await t( r ).ConfigureAwait( false )
                                        : null;
                }
                // The action is the process
                // (it returns a non null object, that is compatible with a object?: use the bang operator).
                return t!;
            }
            // No condition, no action: the inner processor or a void processor.
            if( inner is Func<object, object?> sync )
            {
                return o => ValueTask.FromResult( sync( o ) );
            }
            return Unsafe.As<Func<object, ValueTask<object?>>?>( inner );
        }

        static Func<object, object?>? CreateProcessor( IActivityMonitor monitor,
                                                       IServiceProvider services,
                                                       ObjectPredicateConfiguration? condition,
                                                       ObjectTransformConfiguration? transform,
                                                       ImmutableArray<ObjectProcessorConfiguration> processors )
        {
            Func<object, bool>? c = condition?.CreatePredicate( services );
            Func<object, object>? t = transform?.CreateTransform( services );
            Func<object, object?>? inner = CreateInnerProcessor( monitor, services, processors );
            if( c != null )
            {
                if( t != null )
                {
                    if( inner != null )
                    {
                        // "Full processor" with its conditonned inner processors and action.
                        object? r;
                        return o => c( o )
                                    ? ((r = inner( o )) != null ? t( r ) : null)
                                    : null;
                    }
                    // "Full processor" with its conditonned action.
                    return o => c( o ) ? t( o ) : null;
                }
                if( inner != null )
                {
                    // Condition only processor: the inner processors accept or reject.
                    return o => c( o ) ? inner( o ) : null;
                }
                // Condition only processor: the action is the identity function.
                return o => c( o ) ? o : null;
            }
            // No condition...
            if( t != null )
            {
                if( inner != null )
                {
                    // Applies inner and if it's not null, applies our transform.
                    object? r;
                    return o => (r = inner( o )) != null ? t( r ) : null;
                }
                // The action is the process
                // (it returns a non null object, that is compatible with a object?: use the bang operator).
                return t!;
            }
            // No condition, no action: only the inner processors or a void processor.
            return inner;
        }


        static Func<object, object?>? CreateInnerProcessor( IActivityMonitor monitor,
                                                            IServiceProvider services,
                                                            ImmutableArray<ObjectProcessorConfiguration> processors )
        {
            ImmutableArray<Func<object, object?>> p = processors.Select( c => c.CreateProcessor( monitor, services ) )
                                                                               .Where( f => f != null )
                                                                               .ToImmutableArray()!;
            if( p.Length == 0 ) return null;
            if( p.Length == 1 ) return p[0];
            return o => Apply( p, o );
        }

        static object? CreateHybridInnerProcessor( IActivityMonitor monitor,
                                                   IServiceProvider services,
                                                   ImmutableArray<ObjectProcessorConfiguration> processors )
        {
            var b = ImmutableArray.CreateBuilder<object>( processors.Length );
            bool isSync = true;
            bool isAsync = true;
            bool isFull = true;
            foreach( var p in processors )
            {
                var o = p.CreateHybrid( monitor, services );
                if( o != null )
                {
                    if( o is Func<object, object?> ) isAsync = false;
                    else isSync = false;
                    b.Add( o );
                }
                else isFull = false;
            }
            if( b.Count == 0 ) return null;
            if( b.Count == 1 ) return processors[0];
            var all = isFull ? b.MoveToImmutable() : b.ToImmutable();
            if( isSync )
            {
                var syncs = all.CastArray<Func<object, object?>>();
                return (Func<object, object?>)(o => Apply( syncs, o ));
            }
            if( isAsync )
            {
                var asyncs = all.CastArray<Func<object, ValueTask<object?>>>();
                return (Func<object, ValueTask<object?>>)(async o => await ApplyAsync( asyncs, o ).ConfigureAwait(false) );
            }
            return (Func<object, ValueTask<object?>>)(async o => await ApplyHybridAsync( all, o ).ConfigureAwait( false ));
        }

        static object? Apply( ImmutableArray<Func<object, object?>> processors, object o )
        {
            foreach( var t in processors )
            {
                Throw.DebugAssert( o != null );
                var o2 = t( o )!;
                if( o2 != null ) return o2;
            }
            return null;
        }

        static async ValueTask<object?> ApplyAsync( ImmutableArray<Func<object, ValueTask<object?>>> processors, object o )
        {
            foreach( var t in processors )
            {
                Throw.DebugAssert( o != null );
                var o2 = await t( o ).ConfigureAwait( false )!;
                if( o2 != null ) return o2;
            }
            return null;
        }

        static async ValueTask<object?> ApplyHybridAsync( ImmutableArray<object> processors, object o )
        {
            foreach( var t in processors )
            {
                Throw.DebugAssert( o != null );
                var o2 = o is Func<object, object?> sync
                            ? sync( o )
                            : await (Unsafe.As<Func<object, ValueTask<object?>>>( t ))( o ).ConfigureAwait( false )!;
                if( o2 != null ) return o2;
            }
            return null;
        }

        /// <summary>
        /// Sets the <see cref="IntrinsicCondition"/>.
        /// This must be called only from the configuration constructor.
        /// </summary>
        /// <param name="predicate">The predicate to set.</param>
        protected void SetIntrinsicCondition( ObjectAsyncPredicateConfiguration predicate )
        {
            Throw.CheckState( !_initialized );
            _iCondition = predicate;
        }

        /// <summary>
        /// Sets the <see cref="IntrinsicTransform"/>.
        /// This must be called only from the configuration constructor.
        /// </summary>
        /// <param name="transform">The transformation to set.</param>
        protected void SetIntrinsicTransform( ObjectAsyncTransformConfiguration transform )
        {
            Throw.CheckState( !_initialized );
            _iTransform = transform;
        }

        /// <summary>
        /// Gets the intrinsic condition if this processor implements one.
        /// </summary>
        public ObjectAsyncPredicateConfiguration? IntrinsicCondition
        {
            get
            {
                Initialize();
                return _iCondition;
            }
        }

        /// <summary>
        /// Gets the intrinsic transformation if this processor implements one.
        /// </summary>
        public ObjectAsyncTransformConfiguration? IntrinsicTransform
        {
            get
            {
                Initialize();
                return _iTransform;
            }
        }

        /// <summary>
        /// Creates a <see cref="ObjectProcessorDescriptor"/> (as synchronous capable as possible).
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The descriptor context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured descriptor or null for a void processor.</returns>
        public ObjectProcessorDescriptor? CreateDescriptor( IActivityMonitor monitor, ProcessorDescriptorContext context, IServiceProvider services )
        {
            Initialize();
            var c = _fCondition?.CreateDescriptor( context.ConditionContext, services );
            var t = _fTransform?.CreateDescriptor( context.TransformContext, services );
            ImmutableArray<ObjectProcessorDescriptor> processors = _processors.Select( p => p.CreateDescriptor( monitor, context, services ) )
                                                                              .Where( p => p != null )
                                                                              .ToImmutableArray()!;
            return c != null || t != null || processors.Length > 0
                    ? new ObjectProcessorDescriptor( context, this, c, processors, t )
                    : null;
        }

        sealed class AsyncCond : ObjectAsyncPredicateConfiguration
        {
            readonly Func<IServiceProvider, Func<object, ValueTask<bool>>?> _predicateFactory;
            readonly Func<PredicateDescriptorContext, IServiceProvider, ObjectPredicateDescriptor?>? _descriptorFactory;

            public AsyncCond( string configurationPath,
                              Func<IServiceProvider, Func<object, ValueTask<bool>>?> predicateFactory,
                              Func<PredicateDescriptorContext, IServiceProvider, ObjectPredicateDescriptor?>? descriptorFactory = null )
                : base( configurationPath )
            {
                _predicateFactory = predicateFactory;
                _descriptorFactory = descriptorFactory;
            }

            public override Func<object, ValueTask<bool>>? CreateAsyncPredicate( IServiceProvider services )
            {
                return _predicateFactory( services );
            }

            public override ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
            {
                return _descriptorFactory != null
                        ? _descriptorFactory( context, services )
                        : base.CreateDescriptor( context, services );
            }
        }

        /// <summary>
        /// Sets the <see cref="IntrinsicCondition"/> with an asynchronous condition.
        /// This must be called only from the configuration constructor.
        /// </summary>
        /// <param name="predicateFactory">Required factory of asynchronous condition.</param>
        /// <param name="descriptorFactory">Optional descriptor factory.</param>
        protected void SetIntrinsicAsyncCondition( Func<IServiceProvider, Func<object, ValueTask<bool>>?> predicateFactory,
                                                   Func<PredicateDescriptorContext, IServiceProvider, ObjectPredicateDescriptor?>? descriptorFactory = null )
        {
            Throw.CheckNotNullArgument( predicateFactory );
            SetIntrinsicCondition( new AsyncCond( ConfigurationPath, predicateFactory, descriptorFactory ) );
        }

        sealed class AsyncTrans : ObjectAsyncTransformConfiguration
        {
            readonly Func<IServiceProvider, Func<object, ValueTask<object>>?> _transformFactory;
            readonly Func<TransformDescriptorContext, IServiceProvider, ObjectTransformDescriptor>? _descriptorFactory;

            public AsyncTrans( string configurationPath,
                               Func<IServiceProvider, Func<object, ValueTask<object>>?> transformFactory,
                               Func<TransformDescriptorContext, IServiceProvider, ObjectTransformDescriptor>? descriptorFactory )
                : base( configurationPath )
            {
                _transformFactory = transformFactory;
                _descriptorFactory = descriptorFactory;
            }

            public override Func<object, ValueTask<object>>? CreateAsyncTransform( IServiceProvider services )
            {
                return _transformFactory( services );
            }

            public override ObjectTransformDescriptor? CreateDescriptor( TransformDescriptorContext context, IServiceProvider services )
            {
                return _descriptorFactory != null
                        ? _descriptorFactory( context, services )
                        : base.CreateDescriptor( context, services );
            }
        }

        /// <summary>
        /// Sets the <see cref="IntrinsicTransform"/> with an asynchronous transformation.
        /// This must be called only from the configuration constructor.
        /// </summary>
        /// <param name="transformFactory">Required factory of asynchronous transformation.</param>
        /// <param name="descriptorFactory">Optional descriptor factory.</param>
        protected void SetIntrinsicAsyncTransform( Func<IServiceProvider, Func<object, ValueTask<object>>?> transformFactory,
                                                   Func<TransformDescriptorContext, IServiceProvider, ObjectTransformDescriptor>? descriptorFactory = null )
        {
            Throw.CheckNotNullArgument( transformFactory );
            SetIntrinsicTransform( new AsyncTrans( ConfigurationPath, transformFactory, descriptorFactory ) );
        }

        sealed class Cond : ObjectPredicateConfiguration
        {
            readonly Func<IServiceProvider, Func<object, bool>?> _predicateFactory;
            readonly Func<PredicateDescriptorContext, IServiceProvider, ObjectPredicateDescriptor?>? _descriptorFactory;

            public Cond( string configurationPath,
                         Func<IServiceProvider, Func<object, bool>?> predicateFactory,
                         Func<PredicateDescriptorContext, IServiceProvider, ObjectPredicateDescriptor?>? descriptorFactory = null )
                : base( configurationPath )
            {
                _predicateFactory = predicateFactory;
                _descriptorFactory = descriptorFactory;
            }

            public override Func<object, bool>? CreatePredicate( IServiceProvider services )
            {
                return _predicateFactory( services );
            }

            public override ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
            {
                return _descriptorFactory != null
                        ? _descriptorFactory( context, services )
                        : base.CreateDescriptor( context, services );
            }
        }

        /// <summary>
        /// Sets the <see cref="IntrinsicCondition"/> with a synchronous condition.
        /// This must be called only from the configuration constructor.
        /// </summary>
        /// <param name="predicateFactory">Required factory of synchronous condition.</param>
        /// <param name="descriptorFactory">Optional descriptor factory.</param>
        protected void SetIntrinsicCondition( Func<IServiceProvider, Func<object, bool>?> predicateFactory,
                                              Func<PredicateDescriptorContext, IServiceProvider, ObjectPredicateDescriptor?>? descriptorFactory = null )
        {
            Throw.CheckNotNullArgument( predicateFactory );
            SetIntrinsicCondition( new Cond( ConfigurationPath, predicateFactory, descriptorFactory ) );
        }

        sealed class Trans : ObjectTransformConfiguration
        {
            readonly Func<IServiceProvider, Func<object, object>?> _transformFactory;
            readonly Func<TransformDescriptorContext, IServiceProvider, ObjectTransformDescriptor>? _descriptorFactory;

            public Trans( string configurationPath,
                          Func<IServiceProvider, Func<object, object>?> transformFactory,
                          Func<TransformDescriptorContext, IServiceProvider, ObjectTransformDescriptor>? descriptorFactory = null )
                : base( configurationPath )
            {
                _transformFactory = transformFactory;
                _descriptorFactory = descriptorFactory;
            }

            public override Func<object, object>? CreateTransform( IServiceProvider services )
            {
                return _transformFactory( services );
            }

            public override ObjectTransformDescriptor? CreateDescriptor( TransformDescriptorContext context, IServiceProvider services )
            {
                return _descriptorFactory != null
                        ? _descriptorFactory( context, services )
                        : base.CreateDescriptor( context, services );
            }
        }

        /// <summary>
        /// Sets the <see cref="IntrinsicTransform"/> with a synchronous transformation.
        /// This must be called only from the configuration constructor.
        /// </summary>
        /// <param name="transformFactory">Required factory of synchronous transformation.</param>
        /// <param name="descriptorFactory">Optional descriptor factory.</param>
        protected void SetIntrinsicTransform( Func<IServiceProvider, Func<object, object>?> transformFactory,
                                              Func<TransformDescriptorContext, IServiceProvider, ObjectTransformDescriptor>? descriptorFactory = null )
        {
            Throw.CheckNotNullArgument( transformFactory );
            SetIntrinsicTransform( new Trans( ConfigurationPath, transformFactory, descriptorFactory ) );
        }

        /// <summary>
        /// Adds a <see cref="TypedConfigurationBuilder.TypeResolver"/> for <see cref="ObjectProcessorConfiguration"/>.
        /// <list type="bullet">
        /// <item>The processors must be in the "CK.Object.Processor" namespace.</item>
        /// <item>Their name must end with "ProcessorConfiguration".</item>
        /// </list>
        /// This also calls <see cref="ObjectAsyncPredicateConfiguration.AddResolver(TypedConfigurationBuilder, bool, string)"/>
        /// and <see cref="ObjectAsyncTransformConfiguration.AddResolver(TypedConfigurationBuilder, bool, string)"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Processor" to be specified.</param>
        public static void AddResolver( TypedConfigurationBuilder builder, bool allowOtherNamespace = false )
        {
            // Add the resolvers for Predicates and Transforms.
            ObjectAsyncPredicateConfiguration.AddResolver( builder, allowOtherNamespace );
            ObjectAsyncTransformConfiguration.AddResolver( builder, allowOtherNamespace );
            builder.AddResolver( new TypedConfigurationBuilder.StandardTypeResolver(
                                             baseType: typeof( ObjectProcessorConfiguration ),
                                             typeNamespace: "CK.Object.Processor",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "Processor",
                                             defaultCompositeBaseType: typeof( ObjectProcessorConfiguration ),
                                             compositeItemsFieldName: "Processors" ) );
        }

    }
}
