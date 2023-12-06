using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{

    /// <summary>
    /// Configuration base class for predicates.
    /// <para>
    /// Predicates are always asynchronous but are often simple synchrounous ones by specializing <see cref="ObjectPredicateConfiguration"/>
    /// instead of this base class.
    /// </para>
    /// </summary>
    public abstract partial class ObjectAsyncPredicateConfiguration : IObjectPredicateConfiguration
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
        protected ObjectAsyncPredicateConfiguration( string configurationPath )
        {
            Throw.CheckArgument( MutableConfigurationSection.IsValidPath( configurationPath ) );
            _configurationPath = configurationPath;
        }

        /// <inheritdoc />
        public string ConfigurationPath => _configurationPath;

        /// <summary>
        /// Gets whether this predicate is a synchronous one.
        /// </summary>
        [MemberNotNullWhen( true, nameof( Synchronous ) )]
        public bool IsSynchronous => this is ObjectPredicateConfiguration;

        /// <summary>
        /// Gets this predicate as a synchronous one if it is a synchronous predicate.
        /// </summary>
        public ObjectPredicateConfiguration? Synchronous => this as ObjectPredicateConfiguration;

        /// <summary>
        /// Creates an asynchronous predicate.
        /// </summary>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A configured predicate or null for an empty predicate.</returns>
        public abstract Func<object, ValueTask<bool>>? CreateAsyncPredicate( IServiceProvider services );

        /// <summary>
        /// Creates a <see cref="ObjectPredicateDescriptor"/> with this configuration and a predicate obtained by
        /// calling <see cref="CreateAsyncPredicate(IServiceProvider)"/>.
        /// <para>
        /// This should be overridden if this predicate relies on other predicates in order to hook all of them.
        /// Failing to do so will hide some predicates to the evaluation hook.
        /// </para>
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="services">Services that may be required for some (complex) predicates.</param>
        /// <returns>A descriptor bound to the context or null for an empty predicate.</returns>
        public virtual ObjectPredicateDescriptor? CreateDescriptor( PredicateDescriptorContext context, IServiceProvider services )
        {
            var p = CreateAsyncPredicate( services );
            return p != null ? new ObjectPredicateDescriptor( context, this, p ) : null;
        }

        /// <summary>
        /// Tries to replace a <see cref="PlaceholderPredicateConfiguration"/>.
        /// <para>
        /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that should replace a placeholder.</param>
        /// <returns>A new configuration or null if an error occurred or the placeholder was not found.</returns>
        public ObjectAsyncPredicateConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                                     IConfigurationSection configuration )
        {
            return TrySetPlaceholder( monitor, configuration, out var _ );
        }

        /// <summary>
        /// Tries to replace a <see cref="PlaceholderPredicateConfiguration"/>.
        /// The <paramref name="configuration"/>.Path must be a direct child of the placeholder to replace.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="configuration">The configuration that should replace a placeholder.</param>
        /// <param name="builderError">True if an error occurred while building the configuration, false if the placeholder was not found.</param>
        /// <returns>A new configuration or null if a <paramref name="builderError"/> occurred or the placeholder was not found.</returns>
        public ObjectAsyncPredicateConfiguration? TrySetPlaceholder( IActivityMonitor monitor,
                                                                     IConfigurationSection configuration,
                                                                     out bool builderError )
        {
            builderError = false;
            ObjectAsyncPredicateConfiguration? result = null;
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
        /// Mutator default implementation: always returns this instance by default.
        /// <para>
        /// Errors are emitted in the monitor. On error, this instance is returned. 
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use to signal errors.</param>
        /// <param name="configuration">Configuration of the replaced placeholder.</param>
        /// <returns>A new configuration or this instance if an error occurred or the placeholder has not been found.</returns>
        public virtual ObjectAsyncPredicateConfiguration SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
        {
            return this;
        }

        /// <summary>
        /// Adds a <see cref="TypedConfigurationBuilder.TypeResolver"/> for asynchronous and synchronous predicates.
        /// <list type="bullet">
        /// <item>The predicates must be in the "CK.Object.Predicate" namespace.</item>
        /// <item>Their name must end with "PredicateConfiguration" or "AsyncPredicateConfiguration".</item>
        /// </list>
        /// <para>
        /// If asynchronous predicates must not be supported, use <see cref="ObjectPredicateConfiguration.AddSynchronousOnlyResolver"/>.
        /// </para>
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="allowOtherNamespace">True to allow other namespaces than "CK.Object.Predicate" to be specified.</param>
        /// <param name="compositeItemsFieldName">Name of the composite field.</param>
        /// <returns>The added resolver.</returns>
        public static TypedConfigurationBuilder.StandardTypeResolver AddResolver( TypedConfigurationBuilder builder, bool allowOtherNamespace = false, string compositeItemsFieldName = "Predicates" )
        {
            if( !allowOtherNamespace && compositeItemsFieldName == "Predicates" )
            {
                return AddDefault( builder );
            }
            // First adds the "Sync" family with ObjectPredicateConfiguration as the base type.
            // When explictly resolving a SyncPredicate, only subordinated SyncPredicate are allowed. 
            var sync = new TypedConfigurationBuilder.StandardTypeResolver(
                                             baseType: typeof( ObjectPredicateConfiguration ),
                                             typeNamespace: "CK.Object.Predicate",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "Predicate",
                                             tryCreateFromTypeName: TryCreateFromTypeName,
                                             defaultCompositeBaseType: typeof( GroupPredicateConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName );

            builder.AddResolver( sync );
            // Then adds the more general resolver for base type ObjectAyncPredicateConfiguration.
            // If a "AsyncPredicate" is not found, this resolver falls back to the "Sync" family.
            var async = new TypedConfigurationBuilder.StandardTypeResolver(
                                             baseType: typeof( ObjectAsyncPredicateConfiguration ),
                                             typeNamespace: "CK.Object.Predicate",
                                             allowOtherNamespace: allowOtherNamespace,
                                             familyTypeNameSuffix: "AsyncPredicate",
                                             tryCreateFromTypeName: TryCreateAsyncFromTypeName,
                                             defaultCompositeBaseType: typeof( GroupAsyncPredicateConfiguration ),
                                             compositeItemsFieldName: compositeItemsFieldName,
                                             fallback: sync );
            builder.AddResolver( async );
            return async;

            static TypedConfigurationBuilder.StandardTypeResolver AddDefault( TypedConfigurationBuilder builder )
            {
                // Always add the sync resolver!
                var sync = ObjectPredicateConfiguration.AddSynchronousOnlyResolver( builder );
                if( _defaultAsyncResolver == null )
                {
                    lock( _initDefaultLock )
                    {
                        _defaultAsyncResolver ??= new TypedConfigurationBuilder.StandardTypeResolver(
                                                            baseType: typeof( ObjectAsyncPredicateConfiguration ),
                                                            typeNamespace: "CK.Object.Predicate",
                                                            familyTypeNameSuffix: "AsyncPredicate",
                                                            tryCreateFromTypeName: TryCreateAsyncFromTypeName,
                                                            defaultCompositeBaseType: typeof( GroupAsyncPredicateConfiguration ),
                                                            compositeItemsFieldName: "Predicates",
                                                            fallback: sync );
                    }
                }
                builder.AddResolver( _defaultAsyncResolver );
                return _defaultAsyncResolver;
            }
        }

        private protected static object? TryCreateFromTypeName( IActivityMonitor monitor,
                                                                string typeName,
                                                                TypedConfigurationBuilder builder,
                                                                ImmutableConfigurationSection configuration )
        {
            if( typeName.Equals( "true", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysTruePredicateConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "false", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysFalsePredicateConfiguration( monitor, builder, configuration );
            }
            // This avoids reflection for this known type (not required).
            if( typeName.Equals( "Placeholder", StringComparison.OrdinalIgnoreCase ) )
            {
                return new PlaceholderPredicateConfiguration( monitor, builder, configuration );
            }
            // This will be called only if an explicit synchronous parent is resolved.
            // All these type names have already been resolved by the async resolver and have optimized
            // their returned group with a SyncGroup when all the predicates were sync ones.
            if( typeName.Equals( "All", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.FindItemsSectionAndCreateItems<ObjectPredicateConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedAny( monitor, configuration );
                return new GroupPredicateConfiguration( 0, 0, configuration.Path, items.ToImmutableArray() );
            }
            if( typeName.Equals( "Any", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.FindItemsSectionAndCreateItems<ObjectPredicateConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedSingle( monitor, configuration );
                return new GroupPredicateConfiguration( 1, 0, configuration.Path, items.ToImmutableArray() );
            }
            if( typeName.Equals( "Single", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.FindItemsSectionAndCreateItems<ObjectPredicateConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedAtLeastAtMost( monitor, configuration );
                return new GroupPredicateConfiguration( 1, 1, configuration.Path, items.ToImmutableArray() );
            }
            return null;
        }

        static object? TryCreateAsyncFromTypeName( IActivityMonitor monitor,
                                                   string typeName,
                                                   TypedConfigurationBuilder builder,
                                                   ImmutableConfigurationSection configuration )
        {
            // Handling "true", "false" and "Placeholder" is not required: the sync fallback whould have resolved
            // them anyway but this avoids the second lookup.
            if( typeName.Equals( "true", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysTruePredicateConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "false", StringComparison.OrdinalIgnoreCase ) )
            {
                return new AlwaysFalsePredicateConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "Placeholder", StringComparison.OrdinalIgnoreCase ) )
            {
                return new PlaceholderPredicateConfiguration( monitor, builder, configuration );
            }
            if( typeName.Equals( "All", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.FindItemsSectionAndCreateItems<ObjectAsyncPredicateConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedAny( monitor, configuration );
                return DoCreateGroup( 0, 0, configuration.Path, items );
            }
            if( typeName.Equals( "Any", StringComparison.OrdinalIgnoreCase ) )
            {
                var items = builder.FindItemsSectionAndCreateItems<ObjectAsyncPredicateConfiguration>( monitor, configuration );
                if( items == null ) return null;
                WarnUnusedSingle( monitor, configuration );
                return DoCreateGroup( 1, 0, configuration.Path, items );
            }
            if( typeName.Equals( "Single", StringComparison.OrdinalIgnoreCase ) )
            {
                var predicates = builder.FindItemsSectionAndCreateItems<ObjectAsyncPredicateConfiguration>( monitor, configuration );
                if( predicates == null ) return null;
                WarnUnusedAtLeastAtMost( monitor, configuration );
                return DoCreateGroup( 1, 1, configuration.Path, predicates );
            }
            return null;
        }


        internal static void WarnUnusedAny( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
        {
            if( configuration["Any"] != null )
            {
                monitor.Warn( $"Configuration '{configuration.Path}:Any' is ignored." );
            }
            WarnUnusedSingle( monitor, configuration );
        }

        internal static void WarnUnusedSingle( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
        {
            if( configuration["Single"] != null )
            {
                monitor.Warn( $"Configuration '{configuration.Path}:Single' is ignored." );
            }
            WarnUnusedAtLeastAtMost( monitor, configuration );
        }

        internal static void WarnUnusedAtLeastAtMost( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
        {
            if( configuration["AtLeast"] != null )
            {
                monitor.Warn( $"Configuration '{configuration.Path}:AtLeast' is ignored." );
            }
            if( configuration["AtMost"] != null )
            {
                monitor.Warn( $"Configuration '{configuration.Path}:AtLeast' is ignored." );
            }
        }
    }
}
