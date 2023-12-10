using CK.Core;
using System;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Base class that defines a Type constraint.
    /// <see cref="Type.IsAssignableFrom(Type?)"/> is used.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    public abstract class IsTypePredicateConfiguration<T> : ObjectPredicateConfiguration
    {
        /// <summary>
        /// Required constructor.
        /// </summary>
        /// <param name="monitor">Unused monitor.</param>
        /// <param name="builder">Unused builder.</param>
        /// <param name="configuration">Captured configuration.</param>
        protected IsTypePredicateConfiguration( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )
            : base( configuration.Path )
        {
        }

        /// <inheritdoc />
        public override Func<object, bool> CreatePredicate( IServiceProvider services )
        {
            return static o => typeof(T).IsAssignableFrom( o.GetType() );
        }
    }

}
