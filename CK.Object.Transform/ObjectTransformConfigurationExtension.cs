using CK.Core;
using CK.Object.Transform;
using System;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Extends <see cref="ObjectAsyncTransformConfiguration"/>.
    /// </summary>
    public static class ObjectTransformConfigurationExtension
    {
        /// <summary>
        /// Creates an asynchronous transform that doesn't require any external service to do its job.
        /// <see cref="ObjectAsyncTransformConfiguration.CreateAsyncTransform(IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="this">This transform.</param>
        /// <returns>A configured transform or null for an identity transform.</returns>
        public static Func<object, ValueTask<object>>? CreateAsyncTransform( this ObjectAsyncTransformConfiguration @this )
        {
            return @this.CreateAsyncTransform( EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates an <see cref="ObjectTransformDescriptor"/> that doesn't require any external service to do its job.
        /// <see cref="ObjectAsyncTransformConfiguration.CreateDescriptor(TransformDescriptorContext, IServiceProvider)"/>
        /// is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="this">This transform.</param>
        /// <param name="context">The descriptor context.</param>
        /// <returns>A configured descriptor bound to the descriptor context or null for an identity transform.</returns>
        public static ObjectTransformDescriptor? CreateDescriptor( this ObjectAsyncTransformConfiguration @this, TransformDescriptorContext context )
        {
            return @this.CreateDescriptor( context, EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates a synchronous Transform that doesn't require any external service to do its job.
        /// <see cref="ObjectTransformConfiguration.CreateTransform(IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="this">This transform.</param>
        /// <returns>A configured object Transform or null for an identity transform.</returns>
        public static Func<object, object>? CreateTransform( this ObjectTransformConfiguration @this )
        {
            return @this.CreateTransform( EmptyServiceProvider.Instance );
        }

        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }
    }
}
