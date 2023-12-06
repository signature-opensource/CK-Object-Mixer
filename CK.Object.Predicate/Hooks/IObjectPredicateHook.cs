using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook for a predicate.
    /// </summary>
    public interface IObjectPredicateHook
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IObjectPredicateConfiguration Configuration { get; }

        /// <summary>
        /// Gets this hook as a synchronous one if the predicate is synchronous.
        /// </summary>
        ObjectPredicateHook? Synchronous { get; }

        /// <summary>
        /// Gets the hook context to which this hook is bound.
        /// </summary>
        PredicateHookContext Context { get; }

        /// <summary>
        /// Evaluates the predicate.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        ValueTask<bool> EvaluateAsync( object o );
    }

}
