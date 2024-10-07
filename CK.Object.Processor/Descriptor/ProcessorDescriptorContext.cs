using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace CK.Object.Processor;

/// <summary>
/// Descriptor context for <see cref="ObjectProcessorDescriptor"/>.
/// <para>
/// There is no tracking of processor execution itself: tracking conditions and transforms is verbose enough.
/// </para>
/// </summary>
public class ProcessorDescriptorContext
{
    readonly PredicateDescriptorContext _conditionContext;
    readonly TransformDescriptorContext _transformContext;

    /// <summary>
    /// Initializes a new descriptor context.
    /// </summary>
    /// <param name="conditionContext">Required condition context.</param>
    /// <param name="transformContext">Required transform context.</param>
    public ProcessorDescriptorContext( PredicateDescriptorContext conditionContext, TransformDescriptorContext transformContext )
    {
        Throw.CheckNotNullArgument( conditionContext );
        Throw.CheckNotNullArgument( transformContext );
        _conditionContext = conditionContext;
        _transformContext = transformContext;
    }

    /// <summary>
    /// Gets the context that will be used when evaluating <see cref="ObjectProcessorConfiguration.ConfiguredCondition"/>.
    /// </summary>
    public PredicateDescriptorContext ConditionContext => _conditionContext;

    /// <summary>
    /// Gets the context that will be used when evaluating <see cref="ObjectProcessorConfiguration.ConfiguredTransform"/>.
    /// </summary>
    public TransformDescriptorContext TransformContext => _transformContext;

    /// <summary>
    /// Gets the concatenated errors from <see cref="ConditionContext"/> and <see cref="TransformContext"/>.
    /// </summary>
    public IEnumerable<ExceptionDispatchInfo> Errors => _conditionContext.Errors.Concat( _transformContext.Errors );

    /// <summary>
    /// Gets whether <see cref="ConditionContext"/> or <see cref="TransformContext"/> have error.
    /// </summary>
    public bool HasError => _conditionContext.HasError || _transformContext.HasError;

    /// <summary>
    /// Clears errors from <see cref="ConditionContext"/> and <see cref="TransformContext"/>.
    /// </summary>
    public void ClearErrors()
    {
        _conditionContext.ClearErrors();
        _transformContext.ClearErrors();
    }

}
