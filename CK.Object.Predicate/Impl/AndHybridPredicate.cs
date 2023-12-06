using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    sealed class AndHybridPredicate : ObjectAsyncPredicateConfiguration, IGroupPredicateDescription
    {
        readonly ObjectPredicateConfiguration _left;
        readonly ObjectAsyncPredicateConfiguration _right;
        readonly bool _revert;

        public bool All => true;

        public bool Any => false;

        public bool Single => false;

        public int AtLeast => 0;

        public int AtMost => 0;

        public int PredicateCount => 2;

        public AndHybridPredicate( string configurationPath, ObjectPredicateConfiguration left, ObjectAsyncPredicateConfiguration right, bool revert )
            : base( configurationPath )
        {
            _left = left;
            _right = right;
            _revert = revert;
        }

        public override Func<object, ValueTask<bool>>? CreateAsyncPredicate( IServiceProvider services )
        {
            var l = _left.CreatePredicate( services );
            var r = _right.CreateAsyncPredicate( services );
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

        public override IObjectPredicateHook? CreateAsyncHook( PredicateHookContext context, IServiceProvider services )
        {
            var l = _left.CreateHook( context, services );
            var r = _right.CreateAsyncHook( context, services );
            if( l != null )
            {
                if( r != null )
                {
                    return new HybridPair( context, this, l, r, 0, _revert );
                }
                return l;
            }
            return r;
        }
    }

}
