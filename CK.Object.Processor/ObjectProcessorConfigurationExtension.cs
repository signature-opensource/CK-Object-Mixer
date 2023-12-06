using CK.Core;
using CK.Object.Processor;
using System;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    public static class ObjectProcessorConfigurationExtension
    {
        /// <summary>
        /// Creates an asynchronous transform that doesn't require any external service to do its job.
        /// <para>
        /// <see cref="ObjectProcessorConfiguration.CreateProcessor(IActivityMonitor, IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured processor or null for the void processor.</returns>
        public static Func<object, ValueTask<object?>>? CreateAsyncProcessor( this ObjectProcessorConfiguration @this, IActivityMonitor monitor )
        {
            return @this.CreateAsyncProcessor( monitor, EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates a synchronous processor function that doesn't require any external service to do its job.
        /// <para>
        /// <see cref="ObjectProcessorConfiguration.CreateProcessor(IActivityMonitor, IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </para>
        /// <para>
        /// Must be called only if <see cref="ObjectProcessorConfiguration.IsSynchronous"/> is true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A configured object Processor or null for the void processor.</returns>
        public static Func<object, object?>? CreateProcessor( this ObjectProcessorConfiguration @this, IActivityMonitor monitor )
        {
            return @this.CreateProcessor( monitor, EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates a <see cref="ObjectProcessorDescriptor"/> that doesn't require any external service to do its job.
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured processor hook or null for a void processor.</returns>
        public static ObjectProcessorDescriptor? CreateHook( this ObjectProcessorConfiguration @this, IActivityMonitor monitor, ProcessorDescriptorContext context )
        {
            return @this.CreateDescriptor( monitor, context, EmptyServiceProvider.Instance );
        }


        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }
    }
}
