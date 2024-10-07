using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CK.Object.Transform;

/// <summary>
/// Descriptor context that can track each transformation accross a <see cref="ObjectTransformDescriptor"/> or <see cref="ObjectTransformDescriptor"/>.
/// <para>
/// This base implementation only handles exception thrown by transformation by capturing them in <see cref="Errors"/> and
/// calling <see cref="UserMessageCollector.AppendErrors(Exception, string?, bool?)"/> if a message collector is available.
/// </para>
/// </summary>
public class TransformDescriptorContext
{
    readonly UserMessageCollector? _userMessageCollector;
    List<ExceptionDispatchInfo>? _errors;

    /// <summary>
    /// Initializes a new context.
    /// </summary>
    /// <param name="userMessageCollector">Optional message collector.</param>
    public TransformDescriptorContext( UserMessageCollector? userMessageCollector )
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
    /// Gets whether any error has been captured.
    /// </summary>
    public bool HasError => _errors != null && _errors.Count > 0;

    /// <summary>
    /// Clears any <see cref="Errors"/>.
    /// </summary>
    public void ClearErrors() => _errors?.Clear();

    /// <summary>
    /// Called before evaluating each <see cref="ObjectTransformDescriptor"/> or <see cref="ObjectTransformDescriptor"/>.
    /// <para>
    /// When this method returns a non null object, the transformation is skipped and the result is the returned object.
    /// Implementation should almost always return null except when <paramref name="o"/> is an exception: in this case,
    /// the exception should be returned. This gently propagates any error received by <see cref="OnTransformError(ObjectTransformDescriptor, object, Exception)"/>
    /// up the descriptors.
    /// </para>
    /// <para>
    /// This default implementation propagates the exception if <paramref name="o"/> is an exception.
    /// </para>
    /// </summary>
    /// <param name="source">The source transform.</param>
    /// <param name="o">The object to transform.</param>
    /// <returns>Null to continue the evaluation, a non null object to skip the transformation and substitute the result.</returns>
    internal protected virtual object? OnBeforeTransform( ObjectTransformDescriptor source, object o )
    {
        return o is Exception ? o : null;
    }

    /// <summary>
    /// Called if the evaluation raised an error.
    /// <para>
    /// When null is returned, the exception is rethrown.
    /// When a non null object is returned, it becomes the result of the transformation. 
    /// Implementations should return the exception: whith the help of <see cref="OnBeforeTransform(ObjectTransformDescriptor, object)"/>
    /// the exception will be propagated up to the root descriptor.
    /// </para>
    /// <para>
    /// This default implementation calls <see cref="UserMessageCollector.AppendErrors(Exception, string?, bool?)"/> if a
    /// collector is available and returns the exception.
    /// </para>
    /// </summary>
    /// <param name="source">The source transform.</param>
    /// <param name="o">The object that causes the error.</param>
    /// <param name="ex">The exception raised by the transformation.</param>
    /// <returns>Null to rethrow the exception, a non null object to swallow the result and substitute the result.</returns>
    internal protected virtual object? OnTransformError( ObjectTransformDescriptor source, object o, Exception ex )
    {
        _userMessageCollector?.AppendErrors( ex );
        CaptureError( ex );
        return ex;
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
    /// Called after transformation unless <see cref="OnTransformError(ObjectTransformDescriptor, object, Exception)"/> has been called.
    /// This default implementation returns the <paramref name="result"/> but when overridden this may be changed (but this is unexpected).
    /// <para>
    /// Note that to avoid an illegal null object to be propagated, if this method returns null (that should not happen)
    /// the non null <paramref name="result"/> is returned instead.
    /// </para>
    /// </summary>
    /// <param name="source">The source transform.</param>
    /// <param name="o">The initial object.</param>
    /// <param name="result">The transformed object.</param>
    /// <returns>The <paramref name="result"/>.</returns>
    internal protected virtual object OnAfterTransform( ObjectTransformDescriptor source, object o, object result ) => result;
}
