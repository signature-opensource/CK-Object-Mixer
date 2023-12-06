using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// This is not exposed: the base predicate and interface for the group are enough.
    /// The required constructor must be let public: it is called by reflection for the default composite of the family
    /// and for the regular "Group" type name.
    /// </summary>
    sealed class SequenceTransformConfiguration : ObjectTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly ImmutableArray<ObjectTransformConfiguration> _transforms;

        // Called by reflection when resolving the default composite type of the Sync family and for the "Sequence" type name.
        public SequenceTransformConfiguration( IActivityMonitor monitor,
                                               TypedConfigurationBuilder builder,
                                               ImmutableConfigurationSection configuration,
                                               IReadOnlyList<ObjectTransformConfiguration> transforms )
            : base( configuration.Path )
        {
            _transforms = transforms.ToImmutableArray();
        }

        internal SequenceTransformConfiguration( string configurationPath,
                                                 ImmutableArray<ObjectTransformConfiguration> transforms )
            : base( configurationPath )
        {
            _transforms = transforms;
        }

        public IReadOnlyList<ObjectAsyncTransformConfiguration> Transforms => _transforms;

        public override ObjectTransformDescriptor? CreateDescriptor( TransformDescriptorContext context, IServiceProvider services )
        {
            return SequenceAsyncTransformConfiguration.CreateDescriptor( this, context, services, ImmutableArray<ObjectAsyncTransformConfiguration>.CastUp( _transforms ) );
        }

        /// <inheritdoc />
        public override Func<object, object>? CreateTransform( IServiceProvider services )
        {
            ImmutableArray<Func<object, object>> items = _transforms.Select( c => c.CreateTransform( services ) )
                                                                    .Where( t => t != null )
                                                                    .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            if( items.Length == 2 )
            {
                var f = items[0];
                var s = items[1];
                return o => s( f( o ) );
            }
            return o => Apply( items, o );

            static object Apply( ImmutableArray<Func<object, object>> transformers, object o )
            {
                foreach( var t in transformers )
                {
                    o = t( o );
                }
                return o;
            }
        }

        /// <summary>
        /// Composite mutator.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use to signal errors.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        public override ObjectAsyncTransformConfiguration SetPlaceholder( IActivityMonitor monitor,
                                                                          IConfigurationSection configuration )
        {
            return SequenceAsyncTransformConfiguration.DoSetPlaceholder( monitor, configuration, this, _transforms, ConfigurationPath );
        }

    }

}
