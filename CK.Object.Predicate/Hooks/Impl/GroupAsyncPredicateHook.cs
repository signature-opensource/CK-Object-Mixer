using CK.Core;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    sealed class GroupAsyncPredicateHook : ObjectAsyncPredicateHook, IGroupPredicateHook
    {
        readonly ImmutableArray<IObjectPredicateHook> _predicates;

        public GroupAsyncPredicateHook( PredicateHookContext context, IGroupPredicateConfiguration configuration, ImmutableArray<IObjectPredicateHook> predicates )
            : base( context, configuration )
        {
            Throw.CheckNotNullArgument( predicates );
            _predicates = predicates;
        }

        public new IGroupPredicateConfiguration Configuration => Unsafe.As<IGroupPredicateConfiguration>( base.Configuration );

        public ImmutableArray<IObjectPredicateHook> Predicates => _predicates;

        public bool All => Configuration.All;

        public bool Any => Configuration.Any;

        public bool Single => Configuration.Single;

        public int AtLeast => Configuration.AtLeast;

        public int AtMost => Configuration.AtMost;

        public int PredicateCount => _predicates.Length;

        protected override ValueTask<bool> DoEvaluateAsync( object o )
        {
            var atLeast = Configuration.AtLeast;
            var atMost = Configuration.AtMost;
            if( atMost == 0 )
            {
                return atLeast switch
                {
                    0 => AllAsync( _predicates, o ),
                    1 => AnyAsync( _predicates, o ),
                    _ => AtLeastAsync( _predicates, o, atLeast )
                };
            }
            return MatchBetweenAsync( _predicates, o, atLeast, atMost );
        }

        static async ValueTask<bool> AllAsync( ImmutableArray<IObjectPredicateHook> items, object o )
        {
            foreach( var p in items )
            {
                if( !await p.EvaluateAsync( o ).ConfigureAwait( false ) ) return false;
            }
            return true;
        }

        static async ValueTask<bool> AnyAsync( ImmutableArray<IObjectPredicateHook> items, object o )
        {
            foreach( var p in items )
            {
                if( await p.EvaluateAsync( o ).ConfigureAwait( false ) ) return true;
            }
            return false;
        }

        static async ValueTask<bool> AtLeastAsync( ImmutableArray<IObjectPredicateHook> items, object o, int atLeast )
        {
            int c = 0;
            foreach( var p in items )
            {
                if( await p.EvaluateAsync( o ).ConfigureAwait( false ) )
                {
                    if( ++c == atLeast ) return true;
                }
            }
            return false;
        }

        static async ValueTask<bool> MatchBetweenAsync( ImmutableArray<IObjectPredicateHook> items, object o, int atLeast, int atMost )
        {
            int c = 0;
            foreach( var p in items )
            {
                if( await p.EvaluateAsync( o ).ConfigureAwait( false ) )
                {
                    if( ++c > atMost ) return false;
                }
            }
            return c >= atLeast;
        }

    }

}
