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
            (_atLeast,_atMost) = GroupAsyncPredicateConfiguration.ReadAtLeastAtMost( monitor, configuration, predicates.Count );
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

        public bool Any => _atLeast == 1 && _atMost == 0;

        public bool All => _atLeast == 0 && _atMost == 0;

        public bool Single => _atLeast == 0 && _atMost == 1;

        public int AtLeast => _atLeast;

        public int AtMost => _atMost;

        public IReadOnlyList<ObjectAsyncPredicateConfiguration> Predicates => _predicates;

        /// <inheritdoc />
        public override ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
        {
            return GroupAsyncPredicateConfiguration.CreateDescriptor( this, context, services, ImmutableArray<ObjectAsyncPredicateConfiguration>.CastUp( _predicates ) );
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

        public override ObjectAsyncPredicateConfiguration? SetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration )
        {
            return GroupAsyncPredicateConfiguration.DoSetPlaceholder( monitor, configuration, this, _predicates, _atLeast, _atMost, ConfigurationPath );
        }
    }

}
