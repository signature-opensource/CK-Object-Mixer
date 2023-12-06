using System.Collections.Immutable;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Group hook.
    /// </summary>
    public interface IGroupPredicateHook : IObjectPredicateHook, IGroupPredicateDescription
    {
        /// <summary>
        /// Gets the subordinated predicates.
        /// </summary>
        ImmutableArray<IObjectPredicateHook> Predicates { get; }
    }

}
