using CK.Core;
using CK.Object.Processor;
using System;
using System.Threading.Tasks;

namespace CK.Object.Processor
{
    /// <summary>
    /// Extends <see cref="ObjectProcessorConfiguration"/>.
    /// </summary>
    public static class ObjectProcessorConfigurationExtension
    {
        /// <summary>
        /// Creates an asynchronous transform that doesn't require any external service to do its job.
        /// <para>
        /// <see cref="ObjectProcessorConfiguration.CreateProcessor(IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </para>
        /// </summary>
        /// <param name="this">This processor configuration.</param>
        /// <returns>A configured processor or null for the void processor.</returns>
        public static Func<object, ValueTask<object?>>? CreateAsyncProcessor( this ObjectProcessorConfiguration @this )
        {
            return @this.CreateAsyncProcessor( EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates a synchronous processor function that doesn't require any external service to do its job.
        /// <para>
        /// <see cref="ObjectProcessorConfiguration.CreateProcessor(IServiceProvider)"/> is called
        /// with an empty <see cref="IServiceProvider"/>.
        /// </para>
        /// <para>
        /// Must be called only if <see cref="ObjectProcessorConfiguration.IsSynchronous"/> is true otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </para>
        /// </summary>
        /// <param name="this">This processor configuration.</param>
        /// <returns>A configured object Processor or null for the void processor.</returns>
        public static Func<object, object?>? CreateProcessor( this ObjectProcessorConfiguration @this )
        {
            return @this.CreateProcessor( EmptyServiceProvider.Instance );
        }

        /// <summary>
        /// Creates a <see cref="ObjectProcessorDescriptor"/> that doesn't require any external service to do its job.
        /// </summary>
        /// <param name="this">This processor configuration.</param>
        /// <param name="context">The descriptor context.</param>
        /// <returns>A configured processor descriptor or null for a void processor.</returns>
        public static ObjectProcessorDescriptor? CreateDescriptor( this ObjectProcessorConfiguration @this, ProcessorDescriptorContext context )
        {
            return @this.CreateDescriptor( context, EmptyServiceProvider.Instance );
        }


        internal sealed class EmptyServiceProvider : IServiceProvider
        {
            public static readonly EmptyServiceProvider Instance = new EmptyServiceProvider();
            public object? GetService( Type serviceType ) => null;
        }
    }
}
