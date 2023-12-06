using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook implementation for synchronous predicate.
    /// </summary>
    public class ObjectPredicateHook : IObjectPredicateHook
    {
        readonly PredicateHookContext _context;
        readonly IObjectPredicateConfiguration _configuration;
        readonly Func<object, bool> _predicate;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The predicate configuration.</param>
        /// <param name="predicate">The predicate.</param>
        public ObjectPredicateHook( PredicateHookContext context, IObjectPredicateConfiguration configuration, Func<object, bool> predicate )
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
        /// other <see cref="ObjectPredicateConfiguration"/> to expose the internal predicate structure (and <see cref="DoEvaluate(object)"/>
        /// must be overridden).
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">This configuration.</param>
        protected ObjectPredicateHook( PredicateHookContext context, IObjectPredicateConfiguration configuration )
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
        public PredicateHookContext Context => _context;

        /// <inheritdoc />
        public ObjectPredicateHook? Synchronous => this;

        ValueTask<bool> IObjectPredicateHook.EvaluateAsync( object o ) => ValueTask.FromResult( Evaluate( o ) );

        /// <summary>
        /// Evaluates the predicate. 
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        public bool Evaluate( object o )
        {
            if( !_context.OnBeforePredicate( this, o ) )
            {
                return false;
            }
            bool r = false;
            try
            {
                r = DoEvaluate( o );
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
        /// Actual evaluation of the predicate.
        /// </summary>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>The predicate result.</returns>
        protected virtual bool DoEvaluate( object o ) => _predicate( o );

    }

}
