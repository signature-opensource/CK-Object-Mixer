using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace CK.Object.Processor
{
    /// <summary>
    /// Hook that logs the evaluation details and capture errors.
    /// </summary>
    public class MonitoredProcessorDescriptorContext : ProcessorDescriptorContext
    {
        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="monitor">The monitor that will receive evaluation details.</param>
        /// <param name="tags">Optional tags for log entries.</param>
        /// <param name="groupLevel">Default group level. Use <see cref="LogLevel.None"/> to not open a group for each processor.</param>
        /// <param name="conditionHookContext">Existing condition hook context to use instead of creating a <see cref="MonitoredPredicateDescriptorContext"/>.</param>
        /// <param name="transformHookContext">Existing tranform hook context to use instead of creating a <see cref="MonitoredTransformHookContext"/>.</param>
        public MonitoredProcessorDescriptorContext( IActivityMonitor monitor,
                                                    CKTrait? tags = null,
                                                    LogLevel groupLevel = LogLevel.Trace,
                                                    PredicateHookContext? conditionHookContext = null,
                                                    TransformHookContext? transformHookContext = null )
            : base( conditionHookContext ?? new MonitoredPredicateDescriptorContext( monitor, tags, groupLevel ),
                    transformHookContext ?? new MonitoredTransformHookContext( monitor, tags, groupLevel ) )
        {
        }
    }
}
