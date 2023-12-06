using System;

namespace CK.Object.Predicate
{
    public partial class ObjectAsyncPredicateHook
    {
        /// <summary>
        /// Creates a "And" hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="left">The left hook.</param>
        /// <param name="right">The right hook.</param>
        /// <returns>Left && right hook.</returns>
        public static IObjectPredicateHook CreateAndHook( PredicateHookContext context,
                                                          IObjectPredicateConfiguration configuration,
                                                          IObjectPredicateHook left,
                                                          IObjectPredicateHook right )
        {
            if( left is ObjectPredicateHook sLeft && right is ObjectPredicateHook sRight )
            {
                return new Pair( context, configuration, sLeft, sRight, 0 );
            }
            return new AsyncPair( context, configuration, left, right, 0 );
        }

        /// <summary>
        /// Creates a synchronous "And" hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="left">The left hook.</param>
        /// <param name="right">The right hook.</param>
        /// <returns>Left && right hook.</returns>
        public static ObjectPredicateHook CreateAndHook( PredicateHookContext context,
                                                         IObjectPredicateConfiguration configuration,
                                                         ObjectPredicateHook left,
                                                         ObjectPredicateHook right )
        {
            return new Pair( context, configuration, left, right, 0 );
        }

        /// <summary>
        /// Creates a "Or" hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="left">The left hook.</param>
        /// <param name="right">The right hook.</param>
        /// <returns>Left || right hook.</returns>
        public static IObjectPredicateHook CreateOrHook( PredicateHookContext context,
                                                         IObjectPredicateConfiguration configuration,
                                                         IObjectPredicateHook left,
                                                         IObjectPredicateHook right )
        {
            if( left is ObjectPredicateHook sLeft && right is ObjectPredicateHook sRight )
            {
                return new Pair( context, configuration, sLeft, sRight, 1 );
            }
            return new AsyncPair( context, configuration, left, right, 1 );
        }

        /// <summary>
        /// Creates a "XOr" hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The configuration that defines both left and right.</param>
        /// <param name="left">The left hook.</param>
        /// <param name="right">The right hook.</param>
        /// <returns>Left ^ right hook.</returns>
        public static IObjectPredicateHook CreateXOrHook( PredicateHookContext context,
                                                          IObjectPredicateConfiguration configuration,
                                                          IObjectPredicateHook left,
                                                          IObjectPredicateHook right )
        {
            if( left is ObjectPredicateHook sLeft && right is ObjectPredicateHook sRight )
            {
                return new Pair( context, configuration, sLeft, sRight, 2 );
            }
            return new AsyncPair( context, configuration, left, right, 2 );
        }
    }
}
