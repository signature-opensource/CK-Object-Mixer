using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    sealed class AndAsyncPredicate : ObjectAsyncPredicateConfiguration, IGroupPredicateDescription
    {
        readonly ObjectAsyncPredicateConfiguration _left;
        readonly ObjectAsyncPredicateConfiguration _right;

        public bool All => true;

        public bool Any => false;

        public bool Single => false;

        public int AtLeast => 0;

        public int AtMost => 0;

        public int PredicateCount => 2;

        public AndAsyncPredicate( string configurationPath, ObjectAsyncPredicateConfiguration left, ObjectAsyncPredicateConfiguration right )
            : base( configurationPath )
        {
            _left = left;
            _right = right;
        }

        public override Func<object, ValueTask<bool>>? CreateAsyncPredicate( IServiceProvider services )
        {
            var l = _left.CreateAsyncPredicate( services );
            var r = _right.CreateAsyncPredicate( services );
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

        public override IObjectPredicateHook? CreateAsyncHook( PredicateHookContext context, IServiceProvider services )
        {
            var l = _left.CreateAsyncHook( context, services );
            var r = _right.CreateAsyncHook( context, services );
            if( l != null )
            {
                if( r != null )
                {
                    return new AsyncPair( context, this, l, r, 0 );
                }
                return l;
            }
            return r;
        }
    }

}
