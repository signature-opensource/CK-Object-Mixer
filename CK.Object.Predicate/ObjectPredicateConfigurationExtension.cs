using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    public static class ObjectPredicateConfigurationExtension
    {
        /// <summary>
        /// Creates an asynchronous predicate that doesn't require any external service to do its job.
        /// <see cref="ObjectAsyncPredicateConfiguration.CreateAsyncPredicate(IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured predicate or null for an empty predicate.</returns>
        public static Func<object, ValueTask<bool>>? CreateAsyncPredicate( this ObjectAsyncPredicateConfiguration @this, IActivityMonitor monitor )
        {
            return @this.CreateAsyncPredicate( EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates an <see cref="IObjectPredicateHook"/> that doesn't require any external service to do its job.
        /// <see cref="ObjectAsyncPredicateConfiguration.CreateAsyncHook(PredicateHookContext, IServiceProvider)"/>
        /// is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <returns>A configured wrapper bound to the hook context or null for an empty predicate.</returns>
        public static IObjectPredicateHook? CreateAsyncHook( this ObjectAsyncPredicateConfiguration @this, IActivityMonitor monitor, PredicateHookContext context )
        {
            return @this.CreateAsyncHook( context, EmptyServiceProvider.Instance );
        }


        /// <summary>
        /// Creates a synchronous predicate that doesn't require any external service to do its job.
        /// <see cref="ObjectPredicateConfiguration.CreatePredicate(IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured object predicate or null for an empty predicate.</returns>
        public static Func<object, bool>? CreatePredicate( this ObjectPredicateConfiguration @this, IActivityMonitor monitor )
        {
            return @this.CreatePredicate( EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates an <see cref="ObjectPredicateHook"/> that doesn't require any external service to do its job.
        /// <see cref="ObjectPredicateConfiguration.CreateHook(PredicateHookContext, IServiceProvider)"/> is
        /// called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <returns>A wrapper bound to the hook context or null for an empty predicate.</returns>
        public static ObjectPredicateHook? CreateHook( this ObjectPredicateConfiguration @this, IActivityMonitor monitor, PredicateHookContext context )
        {
            return @this.CreateHook( context, EmptyServiceProvider.Instance );
        }

        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }
    }
}
