using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace CK.Object.Processor
{
    /// <summary>
    /// Descriptor context for <see cref="ObjectProcessorDescriptor"/>.
    /// <para>
    /// There is no tracking of processor execution itself: tracking conditions and transforms is verbose enough.
    /// </para>
    /// </summary>
    public class ProcessorDescriptorContext
    {
        readonly PredicateHookContext _conditionHookContext;
        readonly TransformHookContext _transformHookContext;

        /// <summary>
        /// Initializes a new hook context.
        /// </summary>
        /// <param name="conditionHookContext"></param>
        /// <param name="transformHookContext"></param>
        public ProcessorDescriptorContext( PredicateHookContext conditionHookContext, TransformHookContext transformHookContext )
        {
            Throw.CheckNotNullArgument( conditionHookContext );
            Throw.CheckNotNullArgument( transformHookContext );
            _conditionHookContext = conditionHookContext;
            _transformHookContext = transformHookContext;
        }

        /// <summary>
        /// Gets the hook context that will be used when evaluating <see cref="ObjectProcessorConfiguration.ConfiguredCondition"/>.
        /// </summary>
        public PredicateHookContext ConditionHookContext => _conditionHookContext;

        /// <summary>
        /// Gets the hook context that will be used when evaluating <see cref="ObjectProcessorConfiguration.ConfiguredTransform"/>.
        /// </summary>
        public TransformHookContext TransformHookContext => _transformHookContext;

        /// <summary>
        /// Gets the concatenated errors from <see cref="ConditionHookContext"/> and <see cref="TransformHookContext"/>.
        /// </summary>
        public IEnumerable<ExceptionDispatchInfo> Errors => _conditionHookContext.Errors.Concat( _transformHookContext.Errors );

        /// <summary>
        /// Gets whether <see cref="ConditionHookContext"/> or <see cref="TransformHookContext"/> have error.
        /// </summary>
        public bool HasError => _conditionHookContext.HasError || _transformHookContext.HasError;

        /// <summary>
        /// Clears errors from <see cref="ConditionHookContext"/> and <see cref="TransformHookContext"/>.
        /// </summary>
        public void ClearErrors()
        {
            _conditionHookContext.ClearErrors();
            _transformHookContext.ClearErrors();
        }

    }
}
