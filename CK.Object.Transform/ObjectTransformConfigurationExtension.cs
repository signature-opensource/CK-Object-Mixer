using CK.Core;
using CK.Object.Transform;
using System;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    public static class ObjectTransformConfigurationExtension
    {
        /// <summary>
        /// Creates an asynchronous transform that doesn't require any external service to do its job.
        /// <see cref="ObjectAsyncTransformConfiguration.CreateAsyncTransform(IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured transform or null for an identity transform.</returns>
        public static Func<object, ValueTask<object>>? CreateAsyncTransform( this ObjectAsyncTransformConfiguration @this, IActivityMonitor monitor )
        {
            return @this.CreateAsyncTransform( EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates an <see cref="IObjectTransformHook"/> that doesn't require any external service to do its job.
        /// <see cref="ObjectAsyncTransformConfiguration.CreateAsyncHook(TransformHookContext, IServiceProvider)"/>
        /// is called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <returns>A configured wrapper bound to the hook context or null for an identity transform.</returns>
        public static IObjectTransformHook? CreateAsyncHook( this ObjectAsyncTransformConfiguration @this, IActivityMonitor monitor, TransformHookContext context )
        {
            return @this.CreateAsyncHook( context, EmptyServiceProvider.Instance );
        }


        /// <summary>
        /// Creates a synchronous Transform that doesn't require any external service to do its job.
        /// <see cref="ObjectTransformConfiguration.CreateTransform(IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured object Transform or null for an identity transform.</returns>
        public static Func<object, object>? CreateTransform( this ObjectTransformConfiguration @this, IActivityMonitor monitor )
        {
            return @this.CreateTransform( EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates an <see cref="ObjectTransformHook"/> that doesn't require any external service to do its job.
        /// <see cref="ObjectTransformConfiguration.CreateHook(TransformHookContext, IServiceProvider)"/> is
        /// called with an empty <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <returns>A wrapper bound to the hook context or null for an identity transform.</returns>
        public static ObjectTransformHook? CreateHook( this ObjectTransformConfiguration @this, IActivityMonitor monitor, TransformHookContext context )
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
