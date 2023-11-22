using CK.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CK.Object.Mixer
{
    /// <summary>
    /// Factory for <see cref="IObjectMixer{T}"/> that also configures mixers behavior:
    /// <see cref="IsOutput(object)"/> and <see cref="InputEquals(object, object)"/> must be
    /// overridden.
    /// It initializes itself from a root <see cref="ObjectMixerConfiguration"/>.
    /// </summary>
    public abstract class ObjectMixerFactory
    {
        readonly bool _failOnFirstError;
        readonly int _maxMixCount;
        readonly bool _remixOutput;
        readonly string _inputTypeName;
        readonly string _mixerName;
        readonly ObjectMixerConfiguration _configuration;

        /// <summary>
        /// Initializes a new factory.
        /// </summary>
        /// <param name="monitor">The monitor that signals errors and warning.</param>
        /// <param name="configuration">A root mixer configuration.</param>
        /// <param name="mixerName">Defaults to  <see cref="ImmutableConfigurationSection.Path"/>.</param>
        /// <param name="inputTypeName">Default input type name if the configuration doesn't specify it.</param>
        protected ObjectMixerFactory( IActivityMonitor monitor,
                                      ObjectMixerConfiguration configuration,
                                      string? mixerName = null,
                                      string inputTypeName = "input" )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckNotNullArgument( configuration );

            Throw.DebugAssert( "These must not be changed since they are configuration keys.",
                               nameof( MaxMixCount ) == "MaxMixCount"
                               && nameof( FailOnFirstError ) == "FailOnFirstError"
                               && nameof( RemixOutput ) == "RemixOutput"
                               && nameof( InputTypeName ) == "InputTypeName" );

            _mixerName = string.IsNullOrWhiteSpace( mixerName ) ? configuration.Configuration.Path : mixerName;
            _inputTypeName = configuration.Configuration["InputTypeName"] ?? inputTypeName ?? "input";

            var max = configuration.Configuration.TryGetIntValue( monitor, "MaxMixCount", 1 );
            if( max.HasValue ) _maxMixCount = max.Value;
            else
            {
                _maxMixCount = 100;
                monitor.Info( $"Mixer '{_mixerName}' uses the default MaxMixCount = 100." );
            }
            _failOnFirstError = ReadBoolean( monitor, _mixerName, configuration, "FailOnFirstError", true );
            _remixOutput = ReadBoolean( monitor, _mixerName, configuration, "RemixOutput", true );

            static bool ReadBoolean( IActivityMonitor monitor,
                                     string mixerName,
                                     ObjectMixerConfiguration configuration,
                                     string name,
                                     bool defaultValue )
            {
                var v = configuration.Configuration.TryGetBooleanValue( monitor, name );
                if( v.HasValue ) return v.Value;
                monitor.Info( $"Mixer '{mixerName}' uses the default {name} = {defaultValue}." );
                return defaultValue;
            }
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the root mixer configuration.
        /// </summary>
        public ObjectMixerConfiguration RootConfiguration => _configuration;

        /// <summary>
        /// Gets or whether mixing must fail on first error or continues
        /// the process until its end regardless of errors.
        /// <para>
        /// Defaults to true.
        /// </para>
        /// </summary>
        public bool FailOnFirstError => _failOnFirstError;

        /// <summary>
        /// Gets the maximal number of mix fo an input seed.
        /// Must be greater than 0.
        /// <para>
        /// Defaults to 100.
        /// </para>
        /// </summary>
        public int MaxMixCount => _maxMixCount;

        /// <summary>
        /// Gets or sets whether outputs must be mixed again.
        /// <para>
        /// Defaults to true: outputs are considered like the initial input seed.
        /// </para>
        /// </summary>
        public bool RemixOutput => _remixOutput;

        /// <summary>
        /// Gets the input type name to use.
        /// Defaults to "input".
        /// </summary>
        public string InputTypeName => _inputTypeName;

        /// <summary>
        /// Must returns the name of a type.
        /// By default, this is the object's <see cref="Type.Name"/>.
        /// </summary>
        /// <param name="o">The object whose display name mus be obtained.</param>
        /// <returns>The display name.</returns>
        public virtual string GetDisplayName( object o ) => o.GetType().Name;

        /// <summary>
        /// Gets a name for a mixer.
        /// Defaults to the <see cref="ObjectMixerConfiguration.Configuration"/>.<see cref="ImmutableConfigurationSection.Path">Path</see>.
        /// </summary>
        public virtual ReadOnlySpan<char> GetMixerName( ObjectMixerConfiguration configuration ) => configuration.Configuration.Path;

        /// <summary>
        /// Must implement equality semantics for inputs.
        /// </summary>
        /// <param name="o1">The first input.</param>
        /// <param name="o2">The second input.</param>
        /// <returns>True if they must be considered equals, false otherwise.</returns>
        public abstract bool InputEquals( object o1, object o2 );

        /// <summary>
        /// Predicate that states whether the object satisfies the output.
        /// </summary>
        /// <param name="o">The object to test.</param>
        /// <returns>True if the object is an output, false for an intermediate result.</returns>
        public abstract bool IsOutput( object o );

        /// <summary>
        /// Creates a mixer bound to this factory from <see cref="RootConfiguration"/>.
        /// </summary>
        /// <typeparam name="T">The target output type.</typeparam>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="services">The services.</param>
        /// <returns>The mixer.</returns>
        public ObjectMixer<T> Create<T>( IActivityMonitor monitor, IServiceProvider services ) where T : class
        {
            var mixer = _configuration.CreateMixer( monitor, services );
            return new ObjectMixer<T>( this, mixer );
        }
    }
}
