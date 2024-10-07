using CK.Core;
using CK.Object.Predicate;
using CK.Object.Processor;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Object.Mixer;

/// <summary>
/// Handles optionals "Processor", "OutputCondition" and "MaxProcessCount" configurations.
/// <para>
/// This <c>Type: "ObjectMixer"</c> can be used directly with a configured "Processor" and/or "OutputCondition".
/// </para>
/// <para>
/// Specializations must be in "CK.Object.Mixer" namespace and their type name must end with "MixerConfiguration".
/// </para>
/// <para>
/// This non generic configuration outputs <see cref="Object"/> constrained by the <see cref="OutputType"/>.
/// </para>
/// </summary>
public class ObjectMixerConfiguration : ISupportConfigurationPlaceholder<ObjectMixerConfiguration>
{
    readonly string _name;
    readonly string _configurationPath;
    readonly int _maxProcessCount;
    // These are not readonly because of the Clone method that uses MemberWiseClone:
    // cloned objects are patched with modified configured condition and processor.
    ObjectProcessorConfiguration? _processor;
    ObjectAsyncPredicateConfiguration? _outputCondition;

    /// <summary>
    /// Required constructor when building this non generic "object" mixer configuration from a configuration.
    /// </summary>
    /// <param name="monitor">The monitor to use to signal errors and warnings.</param>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">The configuration section.</param>
    public ObjectMixerConfiguration( IActivityMonitor monitor,
                                     TypedConfigurationBuilder builder,
                                     ImmutableConfigurationSection configuration )
        : this( monitor, builder, configuration, 4 )
    {
    }

    /// <summary>
    /// Constructor for specialization.
    /// </summary>
    /// <param name="monitor">The monitor to use to signal errors and warnings.</param>
    /// <param name="builder">The builder.</param>
    /// <param name="configuration">The configuration section.</param>
    /// <param name="defaultMaxProcessCount">See <see cref="MaxProcessCount"/>.</param>
    protected ObjectMixerConfiguration( IActivityMonitor monitor,
                                        TypedConfigurationBuilder builder,
                                        ImmutableConfigurationSection configuration,
                                        int defaultMaxProcessCount = 4 )
    {
        _name = configuration.Key;
        _configurationPath = configuration.Path;
        var p = configuration.TryGetSection( "Processor" );
        if( p != null ) _processor = builder.Create<ObjectProcessorConfiguration>( monitor, p );
        var c = configuration.TryGetSection( "OutputCondition" );
        if( c != null ) _outputCondition = builder.Create<ObjectAsyncPredicateConfiguration>( monitor, c );
        var max = configuration.TryGetIntValue( monitor, "MaxProcessCount", 1, 10000 );
        _maxProcessCount = max ?? defaultMaxProcessCount;
    }

    /// <summary>
    /// Gets the configured processor if it exists.
    /// </summary>
    public ObjectProcessorConfiguration? Processor => _processor;

    /// <summary>
    /// Gets the configured output condition if it exists.
    /// </summary>
    public ObjectAsyncPredicateConfiguration? OutputCondition => _outputCondition;

    /// <summary>
    /// Gets the configuration path.
    /// </summary>
    public string ConfigurationPath => _configurationPath;

    /// <summary>
    /// Gets the maximal number of processes that can be applied to an initial object
    /// (unless it is accepted) before being rejected (in <see cref="IObjectMixerResult{T}.Rejected"/>).
    /// <para>
    /// Must be between 1 and 1000. Defaults to 4.
    /// </para>
    /// </summary>
    public int MaxProcessCount => _maxProcessCount;

    /// <summary>
    /// Gets the output type (the postcondition).
    /// </summary>
    public virtual Type OutputType => typeof( object );

    /// <summary>
    /// Gets this configuration name that is the <see cref="IConfigurationSection.Key"/>.
    /// </summary>
    public string Name => _name;

    sealed class Result<T> : IObjectMixerResult<T> where T : class
    {
        readonly IObjectMixerResult<object> _r;

        public Result( IObjectMixerResult<object> r )
        {
            _r = r;
        }

        public Exception? Exception => _r.Exception;

        public int TotalProcessCount => _r.TotalProcessCount;

        public object Input => _r.Input;

        public IReadOnlyList<T> Output => (IReadOnlyList<T>)_r.Output;

        public ImmutableArray<(object Object, string Reason)> Rejected => _r.Rejected;
    }

    sealed class Adapter<T> : IObjectMixer<T> where T : class
    {
        readonly ObjectMixer _m;

        public Adapter( ObjectMixer m ) => _m = m;

        public ObjectMixerConfiguration Configuration => _m.Configuration;

        public Task<IObjectMixerResult<T>> MixAsync( IActivityMonitor monitor, object input )
        {
            var tcs = new TaskCompletionSource<IObjectMixerResult<T>>();
            _ = _m.MixAsync( monitor, input ).ContinueWith( t =>
            {
                if( t.IsFaulted ) tcs.TrySetException( t.Exception!.InnerExceptions );
                else if( t.IsCanceled ) tcs.TrySetCanceled();
                else
                {
                    tcs.TrySetResult( new Result<T>( t.Result ) );
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default );
            return tcs.Task;
        }
    }

    /// <summary>
    /// Creates a new mixer with a typed output.
    /// This throws an <see cref="ArgumentException"/> if <typeparamref name="T"/> is not compatible
    /// with <see cref="OutputType"/>.
    /// </summary>
    /// <param name="services">Provides services to the mixer instance.</param>
    /// <returns>A new mixer.</returns>
    /// <typeparam name="T">Type of the mixer output.</typeparam>
    public IObjectMixer<T> Create<T>( IServiceProvider services ) where T : class
    {
        var t = typeof( T );
        if( !OutputType.IsAssignableFrom( t ) )
        {
            Throw.ArgumentException( nameof( T ), $"Generic T argument is '{t.ToCSharpName()}' is not compatible " +
                                                  $"with this mixer OutputType that is '{OutputType.ToCSharpName()}'." );
        }
        var m = Create( services );
        return t == typeof( object ) ? (IObjectMixer<T>)m : new Adapter<T>( m );
    }

    /// <summary>
    /// Creates a new mixer.
    /// </summary>
    /// <param name="services">Provides services to the mixer instance.</param>
    /// <returns>A new mixer.</returns>
    public virtual ObjectMixer Create( IServiceProvider services )
    {
        return new ObjectMixer( services, this );
    }

    /// <summary>
    /// Mutator default implementation handles "Processor" and "OutputCondition" mutations.
    /// </summary>
    /// <param name="monitor">The monitor to use to signal errors.</param>
    /// <param name="configuration">Configuration of the replaced placeholder.</param>
    /// <returns>A new configuration (or this object if nothing changed). Should be null only if an error occurred.</returns>
    public ObjectMixerConfiguration? SetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
    {
        ObjectProcessorConfiguration? p = _processor;
        if( p != null )
        {
            p = p.TrySetPlaceholder( monitor, configuration );
            if( p == null ) return null;
        }
        ObjectAsyncPredicateConfiguration? c = _outputCondition;
        if( c != null )
        {
            c = c.TrySetPlaceholder( monitor, configuration );
            if( c == null ) return null;
        }
        return _outputCondition != c || _processor != p
                ? Clone( c, p )
                : this;
    }

    /// <summary>
    /// Clones this object by using <see cref="object.MemberwiseClone()"/>.
    /// This should work almost all the time but if more control is required, this method
    /// can be overridden and a mutation constructor must be specifically designed.
    /// </summary>
    /// <param name="newCondition">The configured condition to consider.</param>
    /// <param name="newProcessor">The configured processor to consider.</param>
    /// <returns>A mutated clone of this mixer configuration.</returns>
    protected virtual ObjectMixerConfiguration? Clone( ObjectAsyncPredicateConfiguration? newCondition, ObjectProcessorConfiguration? newProcessor )
    {
        var o = (ObjectMixerConfiguration)MemberwiseClone();
        o._outputCondition = newCondition;
        o._processor = newProcessor;
        return o;
    }

    /// <summary>
    /// Adds a <see cref="TypedConfigurationBuilder.TypeResolver"/> for <see cref="ObjectMixerConfiguration"/>.
    /// <list type="bullet">
    /// <item>The mixers must be in the "CK.Object.Mixer" namespace.</item>
    /// <item>Their name must end with "MixerConfiguration".</item>
    /// </list>
    /// This also calls <see cref="ObjectProcessorConfiguration.AddResolver(TypedConfigurationBuilder, bool)"/>.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static void AddResolver( TypedConfigurationBuilder builder )
    {
        ObjectProcessorConfiguration.AddResolver( builder );
        builder.AddResolver( new TypedConfigurationBuilder.StandardTypeResolver(
                                         baseType: typeof( ObjectMixerConfiguration ),
                                         typeNamespace: "CK.Object.Mixer",
                                         familyTypeNameSuffix: "Mixer" ) );
    }
}
