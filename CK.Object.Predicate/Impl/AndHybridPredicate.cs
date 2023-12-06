using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    sealed class AndHybridPredicate : ObjectAsyncPredicateConfiguration, IGroupPredicateConfiguration
    {
        readonly ImmutableArray<ObjectAsyncPredicateConfiguration> _p;
        readonly bool _revert;

        public bool All => true;

        public bool Any => false;

        public bool Single => false;

        public int AtLeast => 0;

        public int AtMost => 0;

        public int PredicateCount => 2;

        public IReadOnlyList<ObjectAsyncPredicateConfiguration> Predicates => _p;

        public AndHybridPredicate( string configurationPath, ObjectPredicateConfiguration left, ObjectAsyncPredicateConfiguration right, bool revert )
            : base( configurationPath )
        {
            _p = ImmutableArray.Create( left, right );
            _revert = revert;
        }

        public override Func<object, ValueTask<bool>>? CreateAsyncPredicate( IServiceProvider services )
        {
            var l = Unsafe.As<ObjectPredicateConfiguration>( _p[0] ).CreatePredicate( services );
            var r = _p[1].CreateAsyncPredicate( services );
            if( l != null )
            {
                if( r != null )
                {
                    return _revert
                            ? async o => l( o ) && await r( o ).ConfigureAwait( false )
                            : async o => await r( o ).ConfigureAwait( false ) && l( o );
                }
                return o => ValueTask.FromResult( l( o ) );
            }
            return r;
        }

        public override ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
        {
            var l = _p[0].CreateDescriptor( context, services );
            var r = _p[1].CreateDescriptor( context, services );
            if( l != null )
            {
                if( r != null )
                {
                    var p = _revert ? ImmutableArray.Create( l, r ) : ImmutableArray.Create( r, l );
                    return new ObjectPredicateDescriptor( context, this, p );
                }
                return l;
            }
            return r;
        }
    }
}
