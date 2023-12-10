using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// This is not exposed: the base predicate and interface for the group are enough.
    /// Instead of the constructor this async version uses the public static Create factory
    /// to return a sync group when all predicates are sync.
    /// </summary>
    sealed class GroupAsyncPredicateConfiguration : ObjectAsyncPredicateConfiguration, IGroupPredicateConfiguration
    {
        readonly ImmutableArray<ObjectAsyncPredicateConfiguration> _predicates;
        readonly int _atLeast;
        readonly int _atMost;

        internal GroupAsyncPredicateConfiguration( int knownAtLeast,
                                                   int knownAtMost,
                                                   string configurationPath,
                                                   ImmutableArray<ObjectAsyncPredicateConfiguration> predicates )
            : base( configurationPath )
        {
            Throw.DebugAssert( knownAtLeast >= 0 && (predicates.Length < 2 || knownAtLeast < predicates.Length) );
            Throw.DebugAssert( knownAtMost == 0 || knownAtMost >= knownAtLeast );
            _predicates = predicates;
            _atLeast = knownAtLeast;
            _atMost = knownAtMost;
        }

        // Called by reflection when resolving the default composite type of the Sync family and for the "Group" type name..
        public static ObjectAsyncPredicateConfiguration Create( IActivityMonitor monitor,
                                                                TypedConfigurationBuilder builder,
                                                                ImmutableConfigurationSection configuration,
                                                                IReadOnlyList<ObjectAsyncPredicateConfiguration> predicates )
        {
            var (atLeast, atMost) = ReadAtLeastAtMost( monitor, configuration, predicates.Count );
            return DoCreateGroup( atLeast, atMost, configuration.Path, predicates );
        }

        /// <summary>
        /// This only emits warnings, not errors.
        /// </summary>
        internal static (int, int) ReadAtLeastAtMost( IActivityMonitor monitor, ImmutableConfigurationSection configuration, int predicatesCount )
        {
            int atLeast = 0;
            int atMost = 0;
            var cAny = configuration.TryGetBooleanValue( monitor, "Any" );
            if( cAny.HasValue && cAny.Value )
            {
                atLeast = 1;
                if( configuration["AtLeast"] != null || configuration["AtMost"] != null || configuration["Single"] != null )
                {
                    monitor.Warn( $"Configuration '{configuration.Path}:Any' is true. 'AtLeast', 'AtMost' and 'Single' are ignored." );
                }
            }
            else
            {
                var cSingle = configuration.TryGetBooleanValue( monitor, "Single" );
                if( cSingle.HasValue && cSingle.Value )
                {
                    atMost = atLeast = 1;
                    if( configuration["AtLeast"] != null || configuration["AtMost"] != null )
                    {
                        monitor.Warn( $"Configuration '{configuration.Path}:Single' is true. 'AtLeast' and 'AtMost' are ignored." );
                    }
                }
                else
                {
                    var fM = configuration.TryGetIntValue( monitor, "AtMost", 1 );
                    if( fM.HasValue )
                    {
                        atMost = fM.Value;
                        if( atMost >= predicatesCount )
                        {
                            atMost = 0;
                            monitor.Warn( $"Configuration '{configuration.Path}:AtMost = {fM.Value}' exceeds number of predicates ({predicatesCount}. This is useless." );
                        }
                    }
                    var fL = configuration.TryGetIntValue( monitor, "AtLeast" );
                    if( fL.HasValue )
                    {
                        atLeast = fL.Value;
                        if( atLeast >= predicatesCount )
                        {
                            atLeast = 0;
                            monitor.Warn( $"Configuration '{configuration.Path}:AtLeast = {fL.Value}' exceeds number of predicates ({predicatesCount}. This is useless." );
                        }
                    }
                    if( atMost > 0 && atMost < atLeast )
                    {
                        atMost = atLeast;
                        monitor.Warn( $"Configuration '{configuration.Path}:AtMost' is lower than 'AtLeast'. Considering exactly {atLeast} conditions." );
                    }
                }

            }
            return (atLeast, atMost);
        }


        public bool Any => _atLeast == 1 && _atMost == 0;

        public bool All => _atLeast == 0 && _atMost == 0;

        public bool Single => _atLeast == 0 && _atMost == 1;

        public int AtLeast => _atLeast;

        public int AtMost => _atMost;

        public IReadOnlyList<ObjectAsyncPredicateConfiguration> Predicates => _predicates;

        public override ObjectAsyncPredicateConfiguration? SetPlaceholder( IActivityMonitor monitor,
                                                                           IConfigurationSection configuration )
        {

            return DoSetPlaceholder( monitor, configuration, this, _predicates, _atLeast, _atMost, ConfigurationPath );
        }

        internal static ObjectAsyncPredicateConfiguration? DoSetPlaceholder( IActivityMonitor monitor,
                                                                             IConfigurationSection configuration,
                                                                             ObjectAsyncPredicateConfiguration @this,
                                                                             IReadOnlyList<ObjectAsyncPredicateConfiguration> predicates,
                                                                             int atLeast,
                                                                             int atMost,
                                                                             string configurationPath )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( configuration );

            // Bails out early if we are not concerned.
            if( !ConfigurationSectionExtension.IsChildPath( configurationPath, configuration.Path ) )
            {
                return @this;
            }
            bool hasAsync = false;
            ImmutableArray<ObjectAsyncPredicateConfiguration>.Builder? newItems = null;
            for( int i = 0; i < predicates.Count; i++ )
            {
                var item = predicates[i];
                var r = item.SetPlaceholder( monitor, configuration );
                if( r == null ) return null;
                if( r != item )
                {
                    if( newItems == null )
                    {
                        newItems = ImmutableArray.CreateBuilder<ObjectAsyncPredicateConfiguration>( predicates.Count );
                        newItems.AddRange( predicates.Take( i ) );
                    }
                }
                hasAsync |= r is not ObjectPredicateConfiguration;
                newItems?.Add( r );
            }
            return newItems != null
                    ? (hasAsync
                            ? new GroupAsyncPredicateConfiguration( atLeast, atMost, configurationPath, newItems.MoveToImmutable() )
                            : new GroupPredicateConfiguration( atLeast, atMost, configurationPath, newItems.Cast<ObjectPredicateConfiguration>().ToImmutableArray() ))
                    : @this;
        }

        public override ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
        {
            return CreateDescriptor( this, context, services, _predicates );
        }

        internal static ObjectPredicateDescriptor? CreateDescriptor( IGroupPredicateConfiguration c,
                                                                     PredicateDescriptorContext context,
                                                                     IServiceProvider services,
                                                                     ImmutableArray<ObjectAsyncPredicateConfiguration> predicates )
        {
            ImmutableArray<ObjectPredicateDescriptor> items = predicates.Select( c => c.CreateDescriptor( context, services ) )
                                                                        .Where( d => d != null )
                                                                        .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            return new ObjectPredicateDescriptor( context, c, items );
        }

        public override Func<object,ValueTask<bool>>? CreateAsyncPredicate( IServiceProvider services )
        {
            ImmutableArray<Func<object, ValueTask<bool>>> items = _predicates.Select( c => c.CreateAsyncPredicate( services ) )
                                                                             .Where( s => s != null )        
                                                                             .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            if( _atMost == 0 )
            {
                return _atLeast switch
                {
                    0 => o => AllAsync( items, o ),
                    1 => o => AnyAsync( items, o ),
                    _ => o => AtLeastAsync( items, o, _atLeast )
                };
            }
            return o => MatchBetweenAsync( items, o, _atLeast, _atMost );
        }

        static async ValueTask<bool> AllAsync( ImmutableArray<Func<object, ValueTask<bool>>> predicates, object o )
        {
            foreach( var p in predicates )
            {
                if( !await p( o ) ) return false;
            }
            return true;
        }

        static async ValueTask<bool> AnyAsync( ImmutableArray<Func<object, ValueTask<bool>>> predicates, object o )
        {
            foreach( var p in predicates )
            {
                if( await p( o ) ) return true;
            }
            return false;
        }

        static async ValueTask<bool> AtLeastAsync( ImmutableArray<Func<object, ValueTask<bool>>> predicates, object o, int atLeast )
        {
            int c = 0;
            foreach( var p in predicates )
            {
                if( await p( o ) )
                {
                    if( ++c == atLeast ) return true; 
                }
            }
            return false;
        }

        static async ValueTask<bool> MatchBetweenAsync( ImmutableArray<Func<object, ValueTask<bool>>> predicates, object o, int atLeast, int atMost )
        {
            int c = 0;
            foreach( var p in predicates )
            {
                if( await p( o ) )
                {
                    if( ++c > atMost ) return false;
                }
            }
            return c >= atLeast;
        }

    }

}
