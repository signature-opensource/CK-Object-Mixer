using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Object.Processor
{
    /// <summary>
    /// Processor placeholder. A placholder is not allowed to have a <see cref="ObjectProcessorConfiguration.ConfiguredCondition"/>
    /// a <see cref="ObjectProcessorConfiguration.ConfiguredTransform"/> or sunbordinated <see cref="ObjectProcessorConfiguration.Processors"/>.
    /// <para>
    /// This always generates a null processor (the void processor).
    /// </para>
    /// </summary>
    public sealed class PlaceholderProcessorConfiguration : ObjectProcessorConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;
        readonly AssemblyConfiguration _assemblies;
        readonly ImmutableArray<TypedConfigurationBuilder.TypeResolver> _resolvers;

        /// <summary>
        /// Initializes a new placeholder.
        /// "Condition" and "Transform" are forbidden.
        /// </summary>
        /// <param name="monitor">The monitor.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The placeholder configuration.</param>
        /// <param name="processors">The (invalid) configured "Processors".</param>
        public PlaceholderProcessorConfiguration( IActivityMonitor monitor,
                                                  TypedConfigurationBuilder builder,
                                                  ImmutableConfigurationSection configuration,
                                                  IReadOnlyList<ObjectProcessorConfiguration> processors )
            : base( monitor, builder, configuration, processors )
        {
            if( ConfiguredCondition != null || ConfiguredTransform != null || processors.Count > 0 )
            {
                monitor.Error( $"A processor Placeholder cannot define a 'Condition', a 'Transform'  or 'Processors' (Configuration '{configuration.Path}')." );
            }
            _configuration = configuration;
            _assemblies = builder.AssemblyConfiguration;
            _resolvers = builder.Resolvers.ToImmutableArray();
        }

        /// <summary>
        /// Returns this or a new processor configuration if <paramref name="configuration"/> is a child
        /// of this configuration.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that will potentially replaces this placeholder.</param>
        /// <returns>A new configuration (or this object if nothing changed). Null only if an error occurred.</returns>
        public override ObjectProcessorConfiguration? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            if( configuration.GetParentPath().Equals( ConfigurationPath, StringComparison.OrdinalIgnoreCase ) )
            {
                var builder = new TypedConfigurationBuilder( _assemblies, _resolvers );
                if( configuration is not ImmutableConfigurationSection config )
                {
                    config = new ImmutableConfigurationSection( configuration, lookupParent: _configuration );
                }
                return builder.Create<ObjectProcessorConfiguration>( monitor, config );
            }
            return this;
        }

    }

}
