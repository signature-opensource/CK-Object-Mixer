using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Security;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Configuration base class for synchronous transform functions.
    /// </summary>
    public abstract class ObjectTransformConfiguration : ObjectAsyncTransformConfiguration
    {
        static readonly object _initDefaultLock = new object();
        static TypedConfigurationBuilder.StandardTypeResolver? _defaultSyncResolver;

        /// <summary>
        /// Captures the configuration section's path (that must be valid, see <see cref="MutableConfigurationSection.IsValidPath(ReadOnlySpan{char})"/>).
        /// <para>
        /// The required signature constructor for specialized class is
        /// <c>( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )</c>.
        /// </para>
        /// </summary>
        /// <param name="configurationPath">The configuration path.</param>
        protected ObjectTransformConfiguration( string configurationPath )
            : base( configurationPath )
        {
        }

        /// <summary>
        /// Adapts this synchronous transform thanks to a <see cref="ValueTask.FromResult{TResult}(TResult)"/>.
        /// </summary>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        public sealed override Func<object, ValueTask<object>>? CreateAsyncTransform( IServiceProvider services )
        {
            var p = CreateTransform( services );
            return p != null ? o => ValueTask.FromResult( p( o ) ) : null;
        }

        /// <summary>
        /// Creates a synchronous transform function.
        /// </summary>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A configured transform function or null for an identity function.</returns>
        public abstract Func<object, object>? CreateTransform( IServiceProvider services );

        /// <summary>
        /// Definite relay to <see cref="CreateHook(TransformHookContext, IServiceProvider)"/>.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A wrapper bound to the hook context or null for the identity transform.</returns>
        public sealed override IObjectTransformHook? CreateAsyncHook( TransformHookContext context, IServiceProvider services )
        {
            return CreateHook( context, services );
        }

        /// <summary>
        /// Creates a <see cref="ObjectTransformHook"/> with this configuration and a function obtained by
        /// calling <see cref="CreateTransform(IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this transform function relies on other transform functions in order to hook all of them.
        /// Failing to do so will hide some transform functions to the evaluation hook.
        /// </para>
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) transform functions.</param>
        /// <returns>A wrapper bound to the hook context or null for an identity function.</returns>
        public virtual ObjectTransformHook? CreateHook( TransformHookContext context, IServiceProvider services )
        {
            var p = CreateTransform( services );
            return p != null ? new ObjectTransformHook( context, this, p ) : null;
        }

        /// <summary>
        /// Adds a <see cref="TypedConfigurationBuilder.TypeResolver"/> for synchronous transform functions only.
        /// <list type="bullet">
        /// <item>The transform functions must be in the "CK.Object.Transform" namespace.</item>
        /// <item>Their name must end with "TransformConfiguration".</item>
        /// </list>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Transform" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        /// <returns>The added resolver.</returns>
        public static TypedConfigurationBuilder.StandardTypeResolver AddSynchronousOnlyResolver( TypedConfigurationBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Transforms" )
        {
            var sync = !allowOtherNamespace && compositeItemsFieldName == "Transforms"
                        ? EnsureDefault()
                        : new TypedConfigurationBuilder.StandardTypeResolver(
                                                baseType: typeof( ObjectTransformConfiguration ),
                                                typeNamespace: "CK.Object.Transform",
                                                allowOtherNamespace: allowOtherNamespace,
                                                familyTypeNameSuffix: "Transform",
                                                defaultCompositeBaseType: typeof( SequenceTransformConfiguration ),
                                                compositeItemsFieldName: compositeItemsFieldName );

            builder.AddResolver( sync );
            return sync;

            static TypedConfigurationBuilder.StandardTypeResolver EnsureDefault()
            {
                if( _defaultSyncResolver == null )
                {
                    lock( _initDefaultLock )
                    {
                        _defaultSyncResolver ??= new TypedConfigurationBuilder.StandardTypeResolver(
                                                            baseType: typeof( ObjectTransformConfiguration ),
                                                            typeNamespace: "CK.Object.Transform",
                                                            familyTypeNameSuffix: "Transform",
                                                            defaultCompositeBaseType: typeof( SequenceTransformConfiguration ),
                                                            compositeItemsFieldName: "Transforms" );
                    }
                }
                return _defaultSyncResolver;
            }
        }

    }
}
