using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate;

/// <summary>
/// Base class for synchronous predicates.
/// </summary>
public static class ObjectPredicateConfigurationExtension
{
    /// <summary>
    /// Creates an asynchronous predicate that doesn't require any external service to do its job.
    /// <see cref="ObjectAsyncPredicateConfiguration.CreateAsyncPredicate(IServiceProvider)"/> is called
    /// with an empty <see cref="IServiceProvider"/>.
    /// </summary>
    /// <returns>A configured predicate or null for an empty predicate.</returns>
    public static Func<object, ValueTask<bool>>? CreateAsyncPredicate( this ObjectAsyncPredicateConfiguration @this )
    {
        return @this.CreateAsyncPredicate( EmptyServiceProvider.Instance );
    }

    /// <summary>
    /// Creates an <see cref="ObjectPredicateDescriptor"/> that doesn't require any external service to do its job.
    /// <see cref="ObjectAsyncPredicateConfiguration.CreateDescriptor(PredicateDescriptorContext, IServiceProvider)"/>
    /// is called with an empty <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="this">This predicate.</param>
    /// <param name="context">The descriptor context.</param>
    /// <returns>A configured descriptor bound to the descriptor context or null for an empty predicate.</returns>
    public static ObjectPredicateDescriptor? CreateDescriptor( this ObjectAsyncPredicateConfiguration @this, PredicateDescriptorContext context )
    {
        return @this.CreateDescriptor( context, EmptyServiceProvider.Instance );
    }

    /// <summary>
    /// Creates a synchronous predicate that doesn't require any external service to do its job.
    /// <see cref="ObjectPredicateConfiguration.CreatePredicate(IServiceProvider)"/> is called
    /// with an empty <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="this">This predicate.</param>
    /// <returns>A configured object predicate or null for an empty predicate.</returns>
    public static Func<object, bool>? CreatePredicate( this ObjectPredicateConfiguration @this )
    {
        return @this.CreatePredicate( EmptyServiceProvider.Instance );
    }

    internal sealed class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
        public object? GetService( Type serviceType ) => null;
    }
}
