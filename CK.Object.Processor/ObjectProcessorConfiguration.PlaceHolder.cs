using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Object.Processor;

public partial class ObjectProcessorConfiguration : ISupportConfigurationPlaceholder<ObjectProcessorConfiguration>
{
    /// <summary>
    /// Clones this object by using <see cref="object.MemberwiseClone()"/>.
    /// This should work almost all the time but if more control is required, this method
    /// can be overridden and a mutation constructor must be specifically designed.
    /// </summary>
    /// <param name="configuredCondition">The configured condition to consider.</param>
    /// <param name="configuredTransform">The configured transform to consider.</param>
    /// <param name="processors">The processors to consider.</param>
    /// <returns>A mutated clone of this processor configuration.</returns>
    internal protected virtual ObjectProcessorConfiguration Clone( ObjectAsyncPredicateConfiguration? configuredCondition,
                                                                   ObjectAsyncTransformConfiguration? configuredTransform,
                                                                   ImmutableArray<ObjectProcessorConfiguration> processors )
    {
        var c = (ObjectProcessorConfiguration)MemberwiseClone();
        c._initialized = false;
        c._cCondition = configuredCondition;
        c._cTransform = configuredTransform;
        c._processors = processors;
        return c;
    }

    /// <summary>
    /// Mutator default implementation handles "Condition", "Transform" and "Processors" mutations.
    /// </summary>
    /// <param name="monitor">The monitor to use to signal errors.</param>
    /// <param name="configuration">Configuration of the replaced placeholder.</param>
    /// <returns>A new configuration (or this object if nothing changed). Null only if an error occurred.</returns>
    public virtual ObjectProcessorConfiguration? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
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
            if( condition == null ) return null;
        }
        // Handles placeholder in Transform.
        var transform = ConfiguredTransform;
        if( transform != null )
        {
            transform = transform.SetPlaceholder( monitor, configuration );
            if( transform == null ) return null;
        }
        // Handles placeholder inside Processors.
        ImmutableArray<ObjectProcessorConfiguration>.Builder? newItems = null;
        for( int i = 0; i < _processors.Length; i++ )
        {
            var item = _processors[i];
            var r = item.SetPlaceholder( monitor, configuration );
            if( r == null ) return r;
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
