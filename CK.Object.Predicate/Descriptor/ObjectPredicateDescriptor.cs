using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Descriptor for predicates.
    /// </summary>
    public sealed class ObjectPredicateDescriptor 
    {
        readonly PredicateDescriptorContext _context;
        readonly IObjectPredicateConfiguration _configuration;
        readonly ImmutableArray<ObjectPredicateDescriptor> _predicates;
        readonly Delegate? _predicate;
        readonly bool _isSync;

        /// <summary>
        /// Initializes a new <see cref="ObjectPredicateDescriptor"/> for a simple, terminal, predicate configuration.
        /// </summary>
        /// <param name="context">The descriptor context.</param>
        /// <param name="configuration">The predicate configuration.</param>
        /// <param name="predicate">The predicate itself: must be a <c>Func&lt;object, bool&gt;</c> or a <c>Func&lt;object,ValueTask&lt;bool&gt;&gt;</c>.</param>
        public ObjectPredicateDescriptor( PredicateDescriptorContext context, IObjectPredicateConfiguration configuration, Delegate predicate )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( predicate );
            _isSync = predicate is Func<object, bool>;
            if( !_isSync && predicate is not Func<object, ValueTask<bool>> )
            {
                Throw.ArgumentException( nameof( predicate ), "Must be Func<object, bool> or Func<object,ValueTask<bool>>." );
            }
            _context = context;
            _configuration = configuration;
            _predicate = predicate;
        }

        internal ObjectPredicateDescriptor( PredicateDescriptorContext context,
                                            IGroupPredicateConfiguration configuration,
                                            ImmutableArray<ObjectPredicateDescriptor> predicates )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckArgument( predicates.Length > 1 );
            _isSync = predicates.All( p => p.IsSynchronous );
            _context = context;
            _configuration = configuration;
            _predicates = predicates;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IObjectPredicateConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets whether this predicate is synchronous or asynchronous.
        /// When true <see cref="EvaluateSync(object)"/> can be called instead of <see cref="EvaluateAsync(object)"/>
        /// </summary>
        public bool IsSynchronous => _isSync;

        /// <summary>
        /// Gets whether this is a group or a simple predicate.
        /// </summary>
        [MemberNotNullWhen( true, nameof( GroupConfiguration ), nameof( Descriptors ) )]
        public bool IsGroup => !_predicates.IsDefault;

        /// <summary>
        /// Gets the description of the group if <see cref="IsGroup"/> is true otherwise null.
        /// </summary>
        public IGroupPredicateConfiguration? GroupConfiguration => _configuration as IGroupPredicateConfiguration;

        /// <summary>
        /// Gets the descriptors of the group if <see cref="IsGroup"/> is true otherwise null.
        /// </summary>
        public IReadOnlyList<ObjectPredicateDescriptor>? Descriptors => _predicates.IsDefault ? null : _predicates;

        /// <summary>
        /// Gets the descriptor context to which this descriptor is bound.
        /// </summary>
        public PredicateDescriptorContext Context => _context;

        /// <summary>
        /// Asynchronously evaluates the predicate.
        /// This is always available.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        public async ValueTask<bool> EvaluateAsync( object o )
        {
            if( _isSync )
            {
                return EvaluateSync( o );
            }
            if( !_context.OnBeforePredicate( this, o ) )
            {
                return false;
            }
            bool r = false;
            try
            {
                if( _predicate != null )
                {
                    r = await Unsafe.As<Func<object, ValueTask<bool>>>( _predicate )( o );
                }
                else
                {
                    r = await GroupEvaluateAsync( o, Unsafe.As<IGroupPredicateConfiguration>( _configuration ), _predicates );
                }
            }
            catch( Exception ex )
            {
                if( _context.OnPredicateError( this, o, ex ) )
                {
                    throw;
                }
            }
            return _context.OnAfterPredicate( this, o, r );

            static ValueTask<bool> GroupEvaluateAsync( object o, IGroupPredicateConfiguration c, ImmutableArray<ObjectPredicateDescriptor> predicates )
            {
                var atLeast = c.AtLeast;
                var atMost = c.AtMost;
                if( atMost == 0 )
                {
                    return atLeast switch
                    {
                        0 => AllAsync( predicates, o ),
                        1 => AnyAsync( predicates, o ),
                        _ => AtLeastAsync( predicates, o, atLeast )
                    };
                }
                return MatchBetweenAsync( predicates, o, atLeast, atMost );

                static async ValueTask<bool> AllAsync( ImmutableArray<ObjectPredicateDescriptor> items, object o )
                {
                    foreach( var p in items )
                    {
                        if( !await p.EvaluateAsync( o ).ConfigureAwait( false ) ) return false;
                    }
                    return true;
                }

                static async ValueTask<bool> AnyAsync( ImmutableArray<ObjectPredicateDescriptor> items, object o )
                {
                    foreach( var p in items )
                    {
                        if( await p.EvaluateAsync( o ).ConfigureAwait( false ) ) return true;
                    }
                    return false;
                }

                static async ValueTask<bool> AtLeastAsync( ImmutableArray<ObjectPredicateDescriptor> items, object o, int atLeast )
                {
                    int c = 0;
                    foreach( var p in items )
                    {
                        if( await p.EvaluateAsync( o ).ConfigureAwait( false ) )
                        {
                            if( ++c == atLeast ) return true;
                        }
                    }
                    return false;
                }

                static async ValueTask<bool> MatchBetweenAsync( ImmutableArray<ObjectPredicateDescriptor> items, object o, int atLeast, int atMost )
                {
                    int c = 0;
                    foreach( var p in items )
                    {
                        if( await p.EvaluateAsync( o ).ConfigureAwait( false ) )
                        {
                            if( ++c > atMost ) return false;
                        }
                    }
                    return c >= atLeast;
                }

            }
        }

        /// <summary>
        /// Synchronously evaluates the predicate. Must be called only if <see cref="IsSynchronous"/> is true
        /// otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        public bool EvaluateSync( object o )
        {
            Throw.CheckState( IsSynchronous );
            if( !_context.OnBeforePredicate( this, o ) )
            {
                return false;
            }
            bool r = false;
            try
            {
                if( _predicate != null )
                {
                    r = Unsafe.As<Func<object,bool>>( _predicate )( o );
                }
                else
                {
                    r = GroupEvaluate( o, Unsafe.As<IGroupPredicateConfiguration>( _configuration ), _predicates );
                }
            }
            catch( Exception ex )
            {
                if( _context.OnPredicateError( this, o, ex ) )
                {
                    throw;
                }
            }
            return _context.OnAfterPredicate( this, o, r );

            static bool GroupEvaluate( object o, IGroupPredicateConfiguration config, ImmutableArray<ObjectPredicateDescriptor> predicates )
            {
                var atLeast = config.AtLeast;
                var atMost = config.AtMost;
                if( atMost == 0 )
                {
                    switch( atLeast )
                    {
                        case 0: return predicates.All( i => i.EvaluateSync( o ) );
                        case 1: return predicates.Any( i => i.EvaluateSync( o ) );
                        default:
                            int c = 0;
                            foreach( var i in predicates )
                            {
                                if( i.EvaluateSync( o ) )
                                {
                                    if( ++c == atLeast ) return true;
                                }
                            }
                            return false;
                    };
                }
                else
                {
                    int c = 0;
                    foreach( var i in predicates )
                    {
                        if( i.EvaluateSync( o ) )
                        {
                            if( ++c > atMost ) return false;
                        }
                    }
                    return c >= atLeast;
                }
            }
        }


    }
}
