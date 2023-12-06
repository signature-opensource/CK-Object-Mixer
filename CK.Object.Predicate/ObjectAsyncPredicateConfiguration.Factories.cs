using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Object.Predicate
{

    public abstract partial class ObjectAsyncPredicateConfiguration
    {
        internal static ObjectAsyncPredicateConfiguration DoCreateGroup( int knownAtLeast,
                                                                         int knownAtMost,
                                                                         string configurationPath,
                                                                         IReadOnlyList<ObjectAsyncPredicateConfiguration> predicates )
        {
            if( predicates.All( p => p is ObjectPredicateConfiguration ) )
            {
                var syncPredicates = predicates.Cast<ObjectPredicateConfiguration>().ToImmutableArray();
                return new GroupPredicateConfiguration( knownAtLeast, knownAtMost, configurationPath, syncPredicates );
            }
            // There is room for improvements here. Instead of simple async choice here, we may split a group in multiple groups,
            // some being sync and some being async and wrap them in a parent group.
            //
            // The idea is to evaluate the fatser sync before the slower async (slower is because of the await state machine).
            //
            // - All would be a "And" pair with a first "All" synchronous group and its second with a "All" asynchronous group.
            // - Any would be a "Or" pair with a first "Any" synchronous group and its second with a "Any" asynchronous group.
            // - Single is less interesting because all predicates must be evaluated. But by grouping sync together and then async
            //   this is still a good thing since we may conclude earlier.
            //
            // Instead of creating intermediate groups (whose hooks would need to be handled carefully if we don't want this restructuration
            // to leak to the end user), a simpler way is to enhance the GroupAsyncPredicateConfiguration:
            // - we can sort the predicates with the sync first and then the async ones and tell the async group about these 2 sub groups.
            // - It can then optimize its work.
            //
            // => We may even remove the sync group and have only a hybrid sync/async group (its Synchronous? property would be dynamic)
            //    but this would change the current architecture tha relies on Sync vs. Async "strong" types.
            //    This is not a good idea.
            //
            return new GroupAsyncPredicateConfiguration( knownAtLeast, knownAtMost, configurationPath, predicates.ToImmutableArray() );
        }

        /// <summary>
        /// Optimally combines 2 optional predicate configuration into a "And" group that will execute as synchronously as possible.
        /// <para>
        /// The order is preserved: <paramref name="left"/> will always be evaluated before <paramref name="right"/>.
        /// </para>
        /// </summary>
        /// <param name="configurationPath">A required configuration path for the combination.</param>
        /// <param name="left">Optional left predicate.</param>
        /// <param name="right">Optional right predicate.</param>
        /// <returns>A "And" group.</returns>
        public static ObjectAsyncPredicateConfiguration? Combine( string configurationPath, ObjectAsyncPredicateConfiguration? left, ObjectAsyncPredicateConfiguration? right )
        {
            if( left != null )
            {
                if( right != null )
                {
                    ObjectPredicateConfiguration? sRight;
                    var sLeft = left.Synchronous;
                    if( sLeft != null )
                    {
                        sRight = right.Synchronous;
                        if( sRight != null )
                        {
                            return new AndSyncPredicate( configurationPath, sLeft, sRight );
                        }
                        return new AndHybridPredicate( configurationPath, sLeft, right, false );
                    }
                    sRight = right.Synchronous;
                    if( sRight != null )
                    {
                        return new AndHybridPredicate( configurationPath, sRight, left, true );
                    }
                    return new AndAsyncPredicate( configurationPath, left, right );
                }
                return left;
            }
            return right;
        }
    }
}
