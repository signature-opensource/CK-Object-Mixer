using CK.Core;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    sealed class HybridPair : ObjectAsyncPredicateHook, IGroupPredicateDescription
    {
        readonly ObjectPredicateHook _left;
        readonly IObjectPredicateHook _right;
        readonly byte _op;
        readonly bool _revert;

        public HybridPair( PredicateHookContext context,
                           IObjectPredicateConfiguration configuration,
                           ObjectPredicateHook left,
                           IObjectPredicateHook right,
                           byte op,
                           bool revert )
            : base( context, configuration )
        {
            Throw.CheckNotNullArgument( left );
            Throw.CheckNotNullArgument( right );
            _left = left;
            _right = right;
            _op = op;
            _revert = revert;
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
            if( _revert )
            {
                return _op switch
                {
                    0 => await _right.EvaluateAsync( o ).ConfigureAwait( false ) && _left.Evaluate( o ),
                    1 => await _right.EvaluateAsync( o ).ConfigureAwait( false ) || _left.Evaluate( o ),
                    2 => await _right.EvaluateAsync( o ).ConfigureAwait( false ) ^ _left.Evaluate( o )
                };
            }
            return _op switch
            {
                0 => _left.Evaluate( o ) && await _right.EvaluateAsync( o ).ConfigureAwait( false ),
                1 => _left.Evaluate( o ) || await _right.EvaluateAsync( o ).ConfigureAwait( false ),
                2 => _left.Evaluate( o ) ^ await _right.EvaluateAsync( o ).ConfigureAwait( false )
            };
#pragma warning restore CS8509
        }
    }

}
