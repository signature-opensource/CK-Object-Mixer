using CK.Core;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    sealed class AsyncPair : ObjectAsyncPredicateHook, IGroupPredicateDescription
    {
        readonly IObjectPredicateHook _left;
        readonly IObjectPredicateHook _right;
        readonly int _op;

        public AsyncPair( PredicateHookContext context,
                          IObjectPredicateConfiguration configuration,
                          IObjectPredicateHook left,
                          IObjectPredicateHook right,
                          int op )
            : base( context, configuration )
        {
            Throw.CheckNotNullArgument( left );
            Throw.CheckNotNullArgument( right );
            _left = left;
            _right = right;
            _op = op;
        }

        public bool All => _op == 0;

        public bool Any => _op == 1;

        public bool Single => _op == 2;

        public int AtLeast => _op > 0 ? 1 : 0;

        public int AtMost => _op == 2 ? 1 : 0;

        public int PredicateCount => 2;

        protected override async ValueTask<bool> DoEvaluateAsync( object o )
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return _op switch
            {
                0 => await _left.EvaluateAsync( o ).ConfigureAwait( false ) && await _right.EvaluateAsync( o ).ConfigureAwait( false ),
                1 => await _left.EvaluateAsync( o ).ConfigureAwait( false ) || await _right.EvaluateAsync( o ).ConfigureAwait( false ),
                2 => await _left.EvaluateAsync( o ).ConfigureAwait( false ) ^ await _right.EvaluateAsync( o ).ConfigureAwait( false )
            };
#pragma warning restore CS8509
        }
    }

}
