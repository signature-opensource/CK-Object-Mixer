using System.Collections.Generic;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Minimal view of a predicate group configuration. <see cref="IGroupPredicateDescription"/>
    /// exposes the constraints.
    /// </summary>
    public interface IGroupPredicateConfiguration : IObjectPredicateConfiguration, IGroupPredicateDescription
    {
        /// <summary>
        /// Gets the subordinated predicates configurations.
        /// <para>
        /// When this is empty, this configuration generates the (null) empty predicate.
        /// Note that this is only configurations. Each of them can generate an empty predicate:
        /// items in this list doens't guaranty anything about the eventual predicate. 
        /// </para>
        /// </summary>
        IReadOnlyList<ObjectAsyncPredicateConfiguration> Predicates { get; }
    }
}
