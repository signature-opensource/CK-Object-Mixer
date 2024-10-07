using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CK.Object.Predicate;

/// <summary>
/// Descriptor context that can track each evaluation accross a <see cref="ObjectPredicateDescriptor"/> or <see cref="ObjectPredicateDescriptor"/>.
/// <para>
/// This base implementation only handles exception thrown by evaluation by capturing them in <see cref="Errors"/> and
/// calling <see cref="UserMessageCollector.AppendErrors(Exception, string?, bool?)"/> if a message collector is available.
/// </para>
/// </summary>
public class PredicateDescriptorContext
{
    readonly UserMessageCollector? _userMessageCollector;
    List<ExceptionDispatchInfo>? _errors;

    /// <summary>
    /// Initializes a new context.
    /// </summary>
    /// <param name="userMessageCollector">Optional message collector.</param>
    public PredicateDescriptorContext( UserMessageCollector? userMessageCollector )
    {
        _userMessageCollector = userMessageCollector;
    }

    /// <summary>
    /// Gets an optional <see cref="UserMessageCollector"/> that can be used to communicate
    /// errors, warnings or information to a end user. 
    /// </summary>
    public UserMessageCollector? UserMessageCollector => _userMessageCollector;

    /// <summary>
    /// Gets the evaluation errors that occurred.
    /// </summary>
    public IReadOnlyList<ExceptionDispatchInfo> Errors => (IReadOnlyList<ExceptionDispatchInfo>?)_errors ?? ImmutableArray<ExceptionDispatchInfo>.Empty;

    /// <summary>
    /// Gets whether at least one error occurred.
    /// When true, <see cref="OnBeforePredicate(ObjectPredicateDescriptor, object)"/> returns false to skip any further evaulations.
    /// </summary>
    public bool HasError => _errors != null && _errors.Count > 0;

    /// <summary>
    /// Clears any <see cref="Errors"/>.
    /// </summary>
    public void ClearErrors() => _errors?.Clear();

    /// <summary>
    /// Called before evaluating each <see cref="ObjectPredicateDescriptor"/> or <see cref="ObjectPredicateDescriptor"/>.
    /// <para>
    /// When this method returns false, the evaluation is skipped with a false result.
    /// Implementations should return false when <see cref="HasError"/> is true. This is what this default implementation does.
    /// </para>
    /// </summary>
    /// <param name="source">The source predicate.</param>
    /// <param name="o">The object to evaluate.</param>
    /// <returns>True to continue the evaluation, false to skip it and return a false result.</returns>
    internal protected virtual bool OnBeforePredicate( ObjectPredicateDescriptor source, object o ) => !HasError;

    /// <summary>
    /// Called if the evaluation raised an error.
    /// <para>
    /// When returning true the exception is rethrown and when returning false the exception is swallowed and the evaluation result is false. 
    /// </para>
    /// <para>
    /// The default implementation at this level considers that the exception is "visible" since it is at least captured in <see cref="Errors"/>,
    /// and when a UserMessageCollector is available, <see cref="UserMessageCollector.AppendErrors(Exception, string?, bool?)"/> is also called:
    /// this method always returns false to avoid rethrowing the exception.
    /// </para>
    /// </summary>
    /// <param name="source">The source predicate.</param>
    /// <param name="o">The object.</param>
    /// <param name="ex">The exception raised by the evaluation.</param>
    /// <returns>True to rethrow the exception, false to swallow it and return a false result.</returns>
    internal protected virtual bool OnPredicateError( ObjectPredicateDescriptor source, object o, Exception ex )
    {
        _userMessageCollector?.AppendErrors( ex );
        CaptureError( ex );
        return false;
    }

    /// <summary>
    /// Captures the exception in <see cref="Errors"/>.
    /// </summary>
    /// <param name="ex">The exception.</param>
    protected void CaptureError( Exception ex )
    {
        _errors ??= new List<ExceptionDispatchInfo>();
        _errors.Add( ExceptionDispatchInfo.Capture( ex ) );
    }

    /// <summary>
    /// Called after predicate evaluation unless <see cref="OnPredicateError(ObjectPredicateDescriptor, object, Exception)"/> has been called.
    /// Implementations should always return the <paramref name="result"/> but when overridden this may be changed (but this is unexpected).
    /// </summary>
    /// <param name="source">The source predicate.</param>
    /// <param name="o">The object.</param>
    /// <param name="result">The evaluated result.</param>
    /// <returns>The <paramref name="result"/>.</returns>
    internal protected virtual bool OnAfterPredicate( ObjectPredicateDescriptor source, object o, bool result ) => result;
}
