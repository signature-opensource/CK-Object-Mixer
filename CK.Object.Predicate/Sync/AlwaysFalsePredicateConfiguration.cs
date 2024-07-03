using CK.Core;
using System;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Simple always false object predicate.
    /// </summary>
    public sealed class AlwaysFalsePredicateConfiguration : ObjectPredicateConfiguration
    {
        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">Unused monitor.</param>
        /// <param name="builder">Unused builder.</param>
        /// <param name="configuration">Captured configuration.</param>
        public AlwaysFalsePredicateConfiguration( IActivityMonitor monitor,
                                                  TypedConfigurationBuilder builder,
                                                  ImmutableConfigurationSection configuration )
            : base( configuration.Path )
        {
        }

        /// <summary>
        /// Always false.
        /// </summary>
        /// <param name="services">Unused.</param>
        /// <returns>A always false predicate.</returns>
        public override Func<object, bool> CreatePredicate( IServiceProvider services )
        {
            return static _ => false;
        }
    }
}
