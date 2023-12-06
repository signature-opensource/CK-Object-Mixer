using CK.Core;

namespace CK.Object.Predicate
{
    sealed class Pair : ObjectPredicateHook, IGroupPredicateDescription
    {
        readonly ObjectPredicateHook _left;
        readonly ObjectPredicateHook _right;
        readonly int _op;

        public Pair( PredicateHookContext context,
                     IObjectPredicateConfiguration configuration,
                     ObjectPredicateHook left,
                     ObjectPredicateHook right,
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

        protected override bool DoEvaluate( object o )
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return _op switch
            {
                0 => _left.Evaluate( o ) && _right.Evaluate( o ),
                1 => _left.Evaluate( o ) || _right.Evaluate( o ),
                2 => _left.Evaluate( o ) ^ _right.Evaluate( o )
            };
#pragma warning restore CS8509
        }
    }
}
