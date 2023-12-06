using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CK.Object.Predicate
{
    /// <summary>
    /// Hook context that logs the evaluation details and capture errors.
    /// </summary>
    public class MonitoredPredicateHookContext : PredicateHookContext
    {
        readonly IActivityMonitor _monitor;
        readonly CKTrait? _tags;
        readonly LogLevel _level;

        /// <summary>
        /// Initializes a new context. Use <paramref name="groupLevel"/> = <see cref="LogLevel.None"/> to not open a group for each predicate:
        /// only error will be logged.
        /// </summary>
        /// <param name="monitor">The monitor that will receive evaluation details.</param>
        /// <param name="tags">Optional tags for log entries.</param>
        /// <param name="groupLevel">Default group level. Use <see cref="LogLevel.None"/> to not open a group for each predicate.</param>
        /// <param name="userMessageCollector">Optional message collector.</param>
        public MonitoredPredicateHookContext( IActivityMonitor monitor,
                                              CKTrait? tags = null,
                                              LogLevel groupLevel = LogLevel.Trace,
                                              UserMessageCollector? userMessageCollector = null )
            : base( userMessageCollector ) 
        {
            Throw.CheckNotNullArgument( monitor );
            _monitor = monitor;
            _tags = tags;
            _level = groupLevel;
        }

        /// <summary>
        /// Gets the log level for log groups. When <see cref="LogLevel.None"/>, no structure is logged, only the
        /// error may be logged.
        /// </summary>
        public LogLevel Level => _level;


        /// <summary>
        /// Checks whether this <see cref="PredicateHookContext.HasError"/> is true and returns false is this case.
        /// Otherwise, a log group of <see cref="Level"/> is opened (the object to evaluate is not logged).
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object to evaluate.</param>
        /// <returns>True if no error occurred to continue the evaluation, false otherwise.</returns>
        internal protected override bool OnBeforePredicate( IObjectPredicateHook source, object o )
        {
            if( HasError ) return false;
            if( _level != LogLevel.None )
            {
                _monitor.OpenGroup( _level, _tags, $"Evaluating '{source.Configuration.ConfigurationPath}'." );
            }
            return true;
        }

        /// <summary>
        /// In addition to the base <see cref="PredicateHookContext.OnPredicateError(IObjectPredicateHook, object, Exception)"/>, this emits
        /// a <see cref="LogLevel.Error"/> with the exception and the <paramref name="o"/> (its <see cref="object.ToString()"/>).
        /// It also always returns false to prevent the exception to be rethrown.
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object.</param>
        /// <param name="ex">The exception raised by the evaluation.</param>
        /// <returns>Always false to prevent the exception to be rethrown.</returns>
        internal protected override bool OnPredicateError( IObjectPredicateHook source, object o, Exception ex )
        {
            base.OnPredicateError( source, o, ex );
            using( _monitor.OpenError( _tags, $"Predicate '{source.Configuration.ConfigurationPath}' error while processing:", ex ) )
            {
                _monitor.Trace( _tags, o?.ToString() ?? "<null>" );
            }
            if( _level != LogLevel.None )
            {
                _monitor.CloseGroup( "Error." );
            }
            return false;
        }

        /// <summary>
        /// Closes the currently opened group with the result as a conclusion.
        /// </summary>
        /// <param name="source">The source predicate.</param>
        /// <param name="o">The object.</param>
        /// <param name="result">The evaluated result.</param>
        /// <returns>The <paramref name="result"/>.</returns>
        internal protected override bool OnAfterPredicate( IObjectPredicateHook source, object o, bool result )
        {
            if( _level != LogLevel.None )
            {
                _monitor.CloseGroup( $"=> {result}" );
            }
            return result;
        }
    }
}
