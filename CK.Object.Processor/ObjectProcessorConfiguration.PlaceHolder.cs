using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Object.Processor
{
    public partial class ObjectProcessorConfiguration
    {
        /// <summary>
        /// Tries to replace a <see cref="PlaceholderProcessorConfiguration"/>.
        /// <para>
        /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that should replace a placeholder.</param>
        /// <returns>A new configuration or null if an error occurred or the placeholder was not found.</returns>
        public ObjectProcessorConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                                IConfigurationSection configuration )
        {
            return TrySetPlaceholder( monitor, configuration, out var _ );
        }

        /// <summary>
        /// Tries to replace a <see cref="PlaceholderProcessorConfiguration"/>.
        /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that should replace a placeholder.</param>
        /// <param name="builderError">True if an error occurred while building the configuration, false if the placeholder was not found.</param>
        /// <returns>A new configuration or null if a <paramref name="builderError"/> occurred or the placeholder was not found.</returns>
        public ObjectProcessorConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                                     IConfigurationSection configuration,
                                                                     out bool builderError )
        {
            builderError = false;
            ObjectProcessorConfiguration? result = null;
            var buildError = false;
            using( monitor.OnError( () => buildError = true ) )
            {
                result = SetPlaceholder( monitor, configuration );
            }
            if( !buildError && result == this )
            {
                monitor.Error( $"Unable to set placeholder: '{configuration.GetParentPath()}' " +
                               $"doesn't exist or is not a placeholder." );
                return null;
            }
            return (builderError = buildError) ? null : result;
        }

        /// <summary>
        /// Mutator default implementation handles "Condition", "Transform" and "Processors" mutations.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use to signal errors.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        public virtual ObjectProcessorConfiguration SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( configuration );
            // Bails out early if we are not concerned.
            if( !ConfigurationSectionExtension.IsChildPath( ConfigurationPath, configuration.Path ) )
            {
                return this;
            }
            // Handles placeholder in Condition.
            var condition = ConfiguredCondition;
            if( condition != null )
            {
                condition = condition.SetPlaceholder( monitor, configuration );
            }
            // Handles placeholder in Transform.
            var transform = ConfiguredTransform;
            if( transform != null )
            {
                transform = transform.SetPlaceholder( monitor, configuration );
            }
            // Handles placeholder inside Processors.
            ImmutableArray<ObjectProcessorConfiguration>.Builder? newItems = null;
            for( int i = 0; i < _processors.Length; i++ )
            {
                var item = _processors[i];
                var r = item.SetPlaceholder( monitor, configuration );
                if( r != item )
                {
                    if( newItems == null )
                    {
                        newItems = ImmutableArray.CreateBuilder<ObjectProcessorConfiguration>( _processors.Length );
                        newItems.AddRange( _processors, i );
                    }
                }
                newItems?.Add( r );
            } 
            var processors = newItems?.ToImmutableArray() ?? _processors;
            return condition != ConfiguredCondition || transform != ConfiguredTransform || processors != _processors
                    ? Clone( condition, transform, processors )
                    : this;
        }
    }
}
