using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Object.Predicate
{
    sealed class AndPredicate : ObjectPredicateConfiguration, IGroupPredicateConfiguration
    {
        readonly ImmutableArray<ObjectPredicateConfiguration> _p;

        public bool All => true;

        public bool Any => false;

        public bool Single => false;

        public int AtLeast => 0;

        public int AtMost => 0;

        public IReadOnlyList<ObjectAsyncPredicateConfiguration> Predicates => _p;

        public AndPredicate( string configurationPath, ObjectPredicateConfiguration left, ObjectPredicateConfiguration right  )
            : base( configurationPath )
        {
            _p = ImmutableArray.Create( left, right );
        }

        public override Func<object, bool>? CreatePredicate( IServiceProvider services )
        {
            var l = _p[0].CreatePredicate( services );
            var r = _p[1].CreatePredicate( services );
            if( l != null )
            {
                if( r != null )
                {
                    return o => l(o) && r(o);
                }
                return l;
            }
            return r;
        }

        public override ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
        {
            return AndAsyncPredicate.CreateDescriptor( this, context, services, ImmutableArray<ObjectAsyncPredicateConfiguration>.CastUp( _p ) );
        }

    }

}
