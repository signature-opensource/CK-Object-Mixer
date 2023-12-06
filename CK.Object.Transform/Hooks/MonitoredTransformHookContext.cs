using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook context that logs the evaluation details and capture errors.
    /// </summary>
    public class MonitoredTransformHookContext : TransformHookContext
    {
        readonly IActivityMonitor _monitor;
        readonly CKTrait? _tags;
        readonly LogLevel _level;

        /// <summary>
        /// Initializes a new hook. Use <paramref name="groupLevel"/> = <see cref="LogLevel.None"/> to not open a group for each transform:
        /// only error will be logged.
        /// </summary>
        /// <param name="monitor">The monitor that will receive evaluation details.</param>
        /// <param name="tags">Optional tags for log entries.</param>
        /// <param name="level">Default group level. Use <see cref="LogLevel.None"/> to not open a group for each transform.</param>
        public MonitoredTransformHookContext( IActivityMonitor monitor,
                                              CKTrait? tags = null,
                                              LogLevel level = LogLevel.Trace,
                                              UserMessageCollector? userMessageCollector = null )
            : base( userMessageCollector )
        {
            Throw.CheckNotNullArgument( monitor );
            _monitor = monitor;
            _tags = tags;
            _level = level;
        }

        /// <summary>
        /// Gets the log level for log groups. When <see cref="LogLevel.None"/>, no structure is logged, only the
        /// error may be logged.
        /// </summary>
        public LogLevel Level => _level;

        /// <summary>
        /// Opens a group. The object to transform is not logged.
        /// <para>
        /// If <paramref name="o"/> is an exception, it is returned as the transformation result.
        /// </para>
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The object to transform.</param>
        /// <returns>
        /// Always null (to continue the transformation) except if the object to evaluate is an exception: it becomes the eventual
        /// transformation result.
        /// </returns>
        internal protected override object? OnBeforeTransform( IObjectTransformHook source, object o )
        {
            if( o is Exception ) return o;
            if( _level != LogLevel.None )
            {
                _monitor.OpenGroup( _level, _tags, $"Evaluating '{source.Configuration.ConfigurationPath}'." );
            }
            return null;
        }

        /// <summary>
        /// In addition to the base <see cref="TransformHookContext.OnTransformError(IObjectTransformHook, object, Exception)"/>, this emits
        /// a <see cref="LogLevel.Error"/> with the exception and the <paramref name="o"/> (its <see cref="object.ToString()"/>).
        /// It also always returns the exception.
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The object that causes the error.</param>
        /// <param name="ex">The exception raised by the transformation.</param>
        /// <returns>The exception.</returns>
        internal protected override object? OnTransformError( IObjectTransformHook source, object o, Exception ex )
        {
            base.OnTransformError( source, o, ex );
            using( _monitor.OpenError( _tags, $"Transform '{source.Configuration.ConfigurationPath}' error while processing:", ex ) )
            {
                _monitor.Trace( _tags, o?.ToString() ?? "<null>" );
            }
            return ex;
        }

        /// <summary>
        /// Closes the currently opened group.
        /// </summary>
        /// <param name="source">The source transform hook.</param>
        /// <param name="o">The initial object.</param>
        /// <param name="result">The transformed object.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        internal protected override object OnAfterTransform( IObjectTransformHook source, object o, object result )
        {
            if( _level != LogLevel.None )
            {
                _monitor.CloseGroup( result is Exception ? "Error." : null );
            }
            return result;
        }
    }
}
