using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Threading.Tasks;

namespace CK.Object.Transform;

/// <summary>
/// Configuration base class for asynchronous transform functions.
/// </summary>
public abstract partial class ObjectAsyncTransformConfiguration : IObjectTransformConfiguration, ISupportConfigurationPlaceholder<ObjectAsyncTransformConfiguration>
{
    readonly string _configurationPath;

    static readonly object _initDefaultLock = new object();
    static TypedConfigurationBuilder.StandardTypeResolver? _defaultAsyncResolver;

    /// <summary>
    /// Captures the configuration section's path (that must be valid, see <see cref="MutableConfigurationSection.IsValidPath(ReadOnlySpan{char})"/>).
    /// <para>
    /// The required signature constructor for specialized class is
    /// <c>( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )</c>.
    /// </para>
    /// </summary>
    /// <param name="configurationPath">The configuration path.</param>
    protected ObjectAsyncTransformConfiguration( string configurationPath )
    {
        _configurationPath = configurationPath;
    }

    /// <inheritdoc />
    public string ConfigurationPath => _configurationPath;

    /// <summary>
    /// Gets whether this transformation as a synchronous one.
    /// </summary>
    [MemberNotNullWhen( true, nameof( Synchronous ) )]
    public bool IsSynchronous => this is ObjectTransformConfiguration;

    /// <summary>
    /// Gets this transformation as a synchronous one if it is a synchronous one.
    /// </summary>
    public ObjectTransformConfiguration? Synchronous => this as ObjectTransformConfiguration;

    /// <summary>
    /// Creates an asynchronous transform function.
    /// </summary>
    /// <param name="services">Services that may be required for some (complex) transform functions.</param>
    /// 
    /// <returns>A configured transform function or null for an identity function.</returns>
    public abstract Func<object, ValueTask<object>>? CreateAsyncTransform( IServiceProvider services );

    /// <summary>
    /// Creates a <see cref="ObjectTransformDescriptor"/> with this configuration and a function obtained by
    /// calling <see cref="CreateAsyncTransform(IServiceProvider)"/>.
    /// <para>
    /// This should be overridden if this transform function relies on other transform functions in order to hook all of them.
    /// Failing to do so will hide some transform descriptor.
    /// </para>
    /// </summary>
    /// <param name="context">The descriptor context.</param>
    /// <param name="services">Services that may be required for some (complex) transform functions.</param>
    /// <returns>A descriptor bound to the descriptor context or null for an identity function.</returns>
    public virtual ObjectTransformDescriptor? CreateDescriptor( TransformDescriptorContext context, IServiceProvider services )
    {
        var p = CreateAsyncTransform( services );
        return p != null ? new ObjectTransformDescriptor( context, this, p ) : null;
    }

    /// <summary>
    /// Mutator default implementation: always returns this instance by default.
    /// </summary>
    /// <param name="monitor">The monitor to use to signal errors.</param>
    /// <param name="configuration">Configuration of the replaced placeholder.</param>
    /// <returns>Always this object.</returns>
    public virtual ObjectAsyncTransformConfiguration? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
    {
        return this;
    }

    /// <summary>
    /// Adds a <see cref="TypedConfigurationBuilder.TypeResolver"/> for asynchronous and synchronous transformers.
    /// <list type="bullet">
    /// <item>The transform functions must be in the "CK.Object.Transform" namespace.</item>
    /// <item>Their name must end with "TransformConfiguration" or "AsyncTransformConfiguration".</item>
    /// </list>
    /// <para>
    /// If asynchronous predicates must not be supported, use <see cref="ObjectTransformConfiguration.AddSynchronousOnlyResolver"/>.
    /// </para>
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Transform" to be specified.</param>
    /// <param name="compositeItemsFieldName">Name of the composite field.</param>
    /// <returns>The added resolver.</returns>
    public static TypedConfigurationBuilder.TypeResolver AddResolver( TypedConfigurationBuilder builder,
                                                                                bool allowOtherNamespace = false,
                                                                                string compositeItemsFieldName = "Transforms" )
    {
        if( !allowOtherNamespace && compositeItemsFieldName == "Transforms" )
        {
            return AddDefault( builder );
        }
        // First adds the "Sync" family with ObjectTransformConfiguration as the base type.
        // When explictly resolving a SyncTransform, only subordinated SyncTransform are allowed. 
        var sync = new TypedConfigurationBuilder.StandardTypeResolver(
                                         baseType: typeof( ObjectTransformConfiguration ),
                                         typeNamespace: "CK.Object.Transform",
                                         allowOtherNamespace: allowOtherNamespace,
                                         familyTypeNameSuffix: "Transform",
                                         defaultCompositeBaseType: typeof( SequenceTransformConfiguration ),
                                         compositeItemsFieldName: compositeItemsFieldName );

        builder.AddResolver( sync );
        // Then adds the more general resolver for base type ObjectAyncTransformConfiguration.
        // If a "AsyncTransform" is not found, this resolver falls back to the "Sync" family.
        var async = new TypedConfigurationBuilder.StandardTypeResolver(
                                         baseType: typeof( ObjectAsyncTransformConfiguration ),
                                         typeNamespace: "CK.Object.Transform",
                                         allowOtherNamespace: allowOtherNamespace,
                                         familyTypeNameSuffix: "AsyncTransform",
                                         defaultCompositeBaseType: typeof( SequenceAsyncTransformConfiguration ),
                                         compositeItemsFieldName: compositeItemsFieldName,
                                         fallback: sync );
        builder.AddResolver( async );
        return async;

        static TypedConfigurationBuilder.StandardTypeResolver AddDefault( TypedConfigurationBuilder builder )
        {
            // Always add the sync resolver!
            var sync = ObjectTransformConfiguration.AddSynchronousOnlyResolver( builder );
            if( _defaultAsyncResolver == null )
            {
                lock( _initDefaultLock )
                {
                    _defaultAsyncResolver ??= new TypedConfigurationBuilder.StandardTypeResolver(
                                                        baseType: typeof( ObjectAsyncTransformConfiguration ),
                                                        typeNamespace: "CK.Object.Transform",
                                                        familyTypeNameSuffix: "AsyncTransform",
                                                        defaultCompositeBaseType: typeof( SequenceAsyncTransformConfiguration ),
                                                        compositeItemsFieldName: "Transforms",
                                                        fallback: sync );
                }
            }
            builder.AddResolver( _defaultAsyncResolver );
            return _defaultAsyncResolver;
        }
    }

}
