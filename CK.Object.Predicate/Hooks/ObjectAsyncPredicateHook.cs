using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook implementation for predicates.
    /// </summary>
    public partial class ObjectAsyncPredicateHook : IObjectPredicateHook
    {
        readonly PredicateHookContext _context;
        readonly IObjectPredicateConfiguration _configuration;
        readonly Func<object, ValueTask<bool>> _predicate;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The predicate configuration.</param>
        /// <param name="predicate">The predicate.</param>
        public ObjectAsyncPredicateHook( PredicateHookContext context, IObjectPredicateConfiguration configuration, Func<object, ValueTask<bool>> predicate )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( predicate );
            _context = context;
            _configuration = configuration;
            _predicate = predicate;
        }

        /// <summary>
        /// Constructor that must be used by specialized hook when the predicate contains
        /// other <see cref="ObjectAsyncPredicateConfiguration"/> to expose the internal predicate structure (and <see cref="DoEvaluateAsync(object)"/>
        /// must be overridden).
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">This configuration.</param>
        protected ObjectAsyncPredicateHook( PredicateHookContext context, IObjectPredicateConfiguration configuration )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            _context = context;
            _configuration = configuration;
            _predicate = null!;
        }

        /// <inheritdoc />
        public IObjectPredicateConfiguration Configuration => _configuration;

        /// <inheritdoc />
        public ObjectPredicateHook? Synchronous => null;

        /// <inheritdoc />
        public PredicateHookContext Context => _context;

        /// <summary>
        /// Evaluates the predicate.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        public async ValueTask<bool> EvaluateAsync( object o )
        {
            if( !_context.OnBeforePredicate( this, o ) )
            {
                return false;
            }
            bool r = false;
            try
            {
                r = await DoEvaluateAsync( o ).ConfigureAwait( false );
            }
            catch( Exception ex )
            {
                if( _context.OnPredicateError( this, o, ex ) )
                {
                    throw;
                }
            }
            return _context.OnAfterPredicate( this, o, r );
        }

        /// <summary>
        /// Evaluates the predicate. 
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        protected virtual ValueTask<bool> DoEvaluateAsync( object o ) => _predicate( o );
    }

}
