using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    sealed class AndAsyncPredicate : ObjectAsyncPredicateConfiguration, IGroupPredicateConfiguration
    {
        readonly ImmutableArray<ObjectAsyncPredicateConfiguration> _p;

        public bool All => true;

        public bool Any => false;

        public bool Single => false;

        public int AtLeast => 0;

        public int AtMost => 0;

        public IReadOnlyList<ObjectAsyncPredicateConfiguration> Predicates => _p;

        public AndAsyncPredicate( string configurationPath, ObjectAsyncPredicateConfiguration left, ObjectAsyncPredicateConfiguration right )
            : base( configurationPath )
        {
            _p = ImmutableArray.Create( left, right );
        }

        public override Func<object, ValueTask<bool>>? CreateAsyncPredicate( IServiceProvider services )
        {
            var l = _p[0].CreateAsyncPredicate( services );
            var r = _p[1].CreateAsyncPredicate( services );
            if( l != null )
            {
                if( r != null )
                {
                    return async o => await l( o ).ConfigureAwait( false ) && await r( o ).ConfigureAwait( false );
                }
                return l;
            }
            return r;
        }

        public override ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
        {
            return CreateDescriptor( this, context, services, _p );
        }

        internal static ObjectPredicateDescriptor? CreateDescriptor( IGroupPredicateConfiguration c,
                                                                     PredicateDescriptorContext context,
                                                                     IServiceProvider services,
                                                                     ImmutableArray<ObjectAsyncPredicateConfiguration> predicates )
        {
            var l = predicates[0].CreateDescriptor( context, services );
            var r = predicates[1].CreateDescriptor( context, services );
            if( l != null )
            {
                if( r != null )
                {
                    var p = ImmutableArray.Create( l, r );
                    return new ObjectPredicateDescriptor( context, c, p );
                }
                return l;
            }
            return r;
        }
    }

}
