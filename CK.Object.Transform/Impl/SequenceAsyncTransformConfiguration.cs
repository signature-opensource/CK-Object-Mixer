using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// This is not exposed: the base predicate and interface for the sequence are enough.
    /// Instead of the constructor this async version uses the public static Create factory
    /// to return a sync sequence when all transforms are sync.
    /// </summary>
    sealed class SequenceAsyncTransformConfiguration : ObjectAsyncTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly ImmutableArray<ObjectAsyncTransformConfiguration> _transforms;

        internal SequenceAsyncTransformConfiguration( string configuration,
                                                      ImmutableArray<ObjectAsyncTransformConfiguration> transforms )
            : base( configuration )
        {
            _transforms = transforms;
        }

        // Called by reflection when resolving the default composite type of the Sync family and for the "Sequence" type name.
        public static ObjectAsyncTransformConfiguration Create( IActivityMonitor monitor,
                                                                TypedConfigurationBuilder builder,
                                                                ImmutableConfigurationSection configuration,
                                                                IReadOnlyList<ObjectAsyncTransformConfiguration> predicates )
        {
            return DoCreateGroup( configuration.Path, predicates );
        }

        public IReadOnlyList<ObjectAsyncTransformConfiguration> Transforms => _transforms;

        public override Func<object,ValueTask<object>>? CreateAsyncTransform( IServiceProvider services )
        {
            ImmutableArray<Func<object, ValueTask<object>>> items = _transforms.Select( c => c.CreateAsyncTransform( services ) )
                                                                               .Where( s => s != null )        
                                                                               .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            return o => Apply( items, o );

            static async ValueTask<object> Apply( ImmutableArray<Func<object, ValueTask<object>>> transformers, object o )
            {
                foreach( var t in transformers )
                {
                    o = await t( o ).ConfigureAwait( false );
                }
                return o;
            }

        }
        public override ObjectAsyncTransformConfiguration? SetPlaceholder( IActivityMonitor monitor,
                                                                           IConfigurationSection configuration )
        {

            return DoSetPlaceholder( monitor, configuration, this, _transforms, ConfigurationPath );
        }

        internal static ObjectAsyncTransformConfiguration? DoSetPlaceholder( IActivityMonitor monitor,
                                                                             IConfigurationSection configuration,
                                                                             ObjectAsyncTransformConfiguration @this,
                                                                             IReadOnlyList<ObjectAsyncTransformConfiguration> predicates,
                                                                             string configurationPath )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( configuration );

            // Bails out early if we are not concerned.
            if( !ConfigurationSectionExtension.IsChildPath( configurationPath, configuration.Path ) )
            {
                return @this;
            }
            bool hasAsync = false;
            ImmutableArray<ObjectAsyncTransformConfiguration>.Builder? newItems = null;
            for( int i = 0; i < predicates.Count; i++ )
            {
                var item = predicates[i];
                var r = item.SetPlaceholder( monitor, configuration );
                if( r == null ) return null;
                if( r != item )
                {
                    if( newItems == null )
                    {
                        newItems = ImmutableArray.CreateBuilder<ObjectAsyncTransformConfiguration>( predicates.Count );
                        newItems.AddRange( predicates.Take( i ) );
                    }
                }
                hasAsync |= r is not ObjectTransformConfiguration;
                newItems?.Add( r );
            }
            return newItems != null
                    ? (hasAsync
                            ? new SequenceAsyncTransformConfiguration( configurationPath, newItems.MoveToImmutable() )
                            : new SequenceTransformConfiguration( configurationPath, newItems.Cast<ObjectTransformConfiguration>().ToImmutableArray() ))
                    : @this;
        }


        public override ObjectTransformDescriptor? CreateDescriptor( TransformDescriptorContext context, IServiceProvider services )
        {
            return CreateDescriptor( this, context, services, _transforms );
        }

        static internal ObjectTransformDescriptor? CreateDescriptor( ISequenceTransformConfiguration c,
                                                                     TransformDescriptorContext context,
                                                                     IServiceProvider services,
                                                                     ImmutableArray<ObjectAsyncTransformConfiguration> transforms )
        {
            ImmutableArray<ObjectTransformDescriptor> items = transforms.Select( c => c.CreateDescriptor( context, services ) )
                                                                        .Where( d => d != null )
                                                                        .ToImmutableArray()!;
            if( items.Length == 0 ) return null;
            if( items.Length == 1 ) return items[0];
            return new ObjectTransformDescriptor( context, c, items );
        }
    }

}
