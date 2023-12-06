using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// This is not exposed: the base predicate and interface for the group are enough.
    /// The required constructor must be let public: it is called by reflection for the default composite of the family
    /// and for the regular "Group" type name.
    /// </summary>
    sealed class GroupPredicateConfiguration : ObjectPredicateConfiguration, IGroupPredicateConfiguration
    {
        readonly ImmutableArray<ObjectPredicateConfiguration> _predicates;
        readonly int _atLeast;
        readonly int _atMost;

        // Called by reflection when resolving the default composite type of the Sync family and for the "Group" type name.
        public GroupPredicateConfiguration( IActivityMonitor monitor,
                                            TypedConfigurationBuilder builder,
                                            ImmutableConfigurationSection configuration,
                                            IReadOnlyList<ObjectPredicateConfiguration> predicates )
            : base( configuration.Path )
        {
            _predicates = predicates.ToImmutableArray();
            (_atLeast,_atMost) = ReadAtLeastAtMost( monitor, configuration, predicates.Count );
        }

        internal GroupPredicateConfiguration( int knownAtLeast,
                                              int knownAtMost,
                                              string configurationPath,
                                              ImmutableArray<ObjectPredicateConfiguration> predicates )
            : base( configurationPath )
        {
            Throw.DebugAssert( knownAtLeast >= 0 && (predicates.Length < 2 || knownAtLeast < predicates.Length) );
            Throw.DebugAssert( knownAtMost == 0 || knownAtMost >= knownAtLeast );
            _predicates = predicates;
            _atLeast = knownAtLeast;
            _atMost = knownAtMost;
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

        IReadOnlyList<ObjectAsyncPredicateConfiguration> IGroupPredicateConfiguration.Predicates => _predicates;

        /// <inheritdoc cref="IGroupPredicateConfiguration.Predicates" />
        public IReadOnlyList<ObjectPredicateConfiguration> Predicates => _predicates;

        /// <inheritdoc />
        public override ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
        {
            ImmutableArray<ObjectPredicateDescriptor> items = _predicates.Select( c => c.CreateDescriptor( context, services ) )
                                                                         .Where( d => d != null )
                                                                         .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            return new ObjectPredicateDescriptor( context, this, items );
        }

        /// <inheritdoc />
        public override Func<object, bool>? CreatePredicate( IServiceProvider services )
        {
            ImmutableArray<Func<object, bool>> items = _predicates.Select( c => c.CreatePredicate( services ) )
                                                                  .Where( f => f != null )
                                                                  .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            // Easy case.
            if( _atMost == 0 )
            {
                return _atLeast switch
                {
                    0 => o => items.All( f => f( o ) ),
                    1 => o => items.Any( f => f( o ) ),
                    _ => o => AtLeastMatch( items, o, _atLeast )
                };
            }
            else
            {
                return o => MatchBetween( items, o, _atLeast, _atMost );
            }

            static bool AtLeastMatch( ImmutableArray<Func<object, bool>> predicates, object o, int atLeast )
            {
                int c = 0;
                foreach( var f in predicates )
                {
                    if( f( o ) )
                    {
                        if( ++c == atLeast ) return true;
                    }
                }
                return false;
            }

            static bool MatchBetween( ImmutableArray<Func<object, bool>> predicates, object o, int atLeast, int atMost )
            {
                int c = 0;
                foreach( var f in predicates )
                {
                    if( f( o ) )
                    {
                        if( ++c > atMost ) return false;
                    }
                }
                return c >= atLeast;
            }
        }

        public override ObjectAsyncPredicateConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration )
        {
            return GroupAsyncPredicateConfiguration.DoSetPlaceholder( monitor, configuration, this, _predicates, _atLeast, _atMost, ConfigurationPath );
        }
    }

}
