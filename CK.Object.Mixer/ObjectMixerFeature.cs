using CK.AppIdentity;
using CK.Core;
using CK.Object.Processor;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Object.Mixer
{
    /// <summary>
    /// The mixer feature holds factories of configured mixers.
    /// </summary>
    public sealed class ObjectMixerFeature
    {
        readonly IParty _party;
        readonly ImmutableArray<Factory> _configurations;

        /// <summary>
        /// Mixer factory: the <see cref="Current"/> configuration can evolve by replacing placeholders in it.
        /// </summary>
        public sealed class Factory
        {
            ObjectMixerConfiguration _current;
            readonly ObjectMixerConfiguration _initial;

            internal Factory( ObjectMixerConfiguration initial )
            {
                _current = _initial = initial;
            }

            /// <summary>
            /// Gets this mixer's name.
            /// </summary>
            public string Name => _current.Name;

            /// <summary>
            /// Gets this mixer's output type.
            /// </summary>
            public Type OutputType => _current.OutputType;

            /// <summary>
            /// Gets the initial configuration, before any successful <see cref="TrySetPlaceholder(IActivityMonitor, IConfigurationSection)"/>.
            /// </summary>
            public ObjectMixerConfiguration Initial => _initial;

            /// <summary>
            /// Gets the current configuration that is different from the <see cref="Initial"/> one when
            /// placeholders have been replaced.
            /// </summary>
            public ObjectMixerConfiguration Current => _current;

            /// <summary>
            /// Creates a new mixer with a typed output.
            /// This throws an <see cref="ArgumentException"/> if <typeparamref name="T"/> is not compatible
            /// with <see cref="OutputType"/>.
            /// </summary>
            /// <param name="services">Provides services to the mixer instance.</param>
            /// <returns>A new mixer.</returns>
            /// <typeparam name="T">Type of the mixer output.</typeparam>
            public IObjectMixer<T> CreateMixer<T>( IServiceProvider services ) where T : class => Current.Create<T>( services );

            /// <summary>
            /// Creates a new mixer.
            /// </summary>
            /// <param name="services">Provides services to the mixer instance.</param>
            /// <returns>A new mixer.</returns>
            public ObjectMixer CreateMixer( IServiceProvider services ) => Current.Create( services );

            /// <summary>
            /// Resets the <see cref="Current"/> to the <see cref="Initial"/>, optionally applying any number of placeholders.
            /// <para>
            /// Placeholder replacement must be successful otherwise false is returned and Current is unchanged.
            /// To be successful the placeholder location must be found and the initialization of the configuration
            /// object must not emit any error.
            /// </para>
            /// <para>
            /// This is an atomic operation.
            /// </para>
            /// </summary>
            /// <param name="monitor">The monitor.</param>
            /// <param name="configurations">Optional placeholder configurations to apply.</param>
            /// <returns>True on success, false is a placholder replacement failed.</returns>
            public bool ResetCurrent( IActivityMonitor monitor, params IConfigurationSection[] configurations )
            {
                Throw.CheckNotNullArgument( monitor );
                if( configurations.Length == 0 )
                {
                    Interlocked.Exchange( ref _current, _initial );
                    return true;
                }
                // See TrySetPlaceholder below for implementation explanations.
                bool success = true;
                var newC = Util.InterlockedSet( ref _current, c =>
                {
                    success = true;
                    ObjectMixerConfiguration? newCurrent = c;
                    foreach( var config in configurations )
                    {
                        newCurrent = c.TrySetPlaceholder( monitor, config );
                        if( newCurrent == null )
                        {
                            success = false;
                            return c;
                        }
                    }
                    return newCurrent;
                } );
                return success;
            }

            /// <summary>
            /// Attempts to update <see cref="Current"/> by replacing a placeholder.
            /// To be successful the placeholder location must be found and the initialization of the configuration
            /// object must not emit any error.
            /// <para>
            /// This is an atomic operation.
            /// </para>
            /// </summary>
            /// <param name="monitor">The monitor.</param>
            /// <param name="configuration">Placeholder configuration to apply.</param>
            /// <returns>True on success, false is a placholder replacement failed.</returns>
            public bool TrySetPlaceholder( IActivityMonitor monitor, IConfigurationSection configuration )
            {
                Throw.CheckNotNullArgument( monitor );
                Throw.CheckNotNullArgument( configuration );
                // Initialization required only for the compiler that cannot be sure that
                // the lambda is called.
                bool success = true;
                var newC = Util.InterlockedSet( ref _current, c =>
                {
                    // Success must be set here: this lambda can be called multiple times.
                    success = true;
                    var newCurrent = c.TrySetPlaceholder( monitor, configuration );
                    if( newCurrent == null )
                    {
                        // When an error occurred, the current Current is returned
                        // unchanged. TrySetPlaceholder guaranties that an error has been logged.
                        success = false;
                        return c;
                    }
                    return newCurrent;
                } );
                return success;
            }

        }

        internal ObjectMixerFeature( IParty party, ImmutableArray<Factory> configurations )
        {
            _party = party;
            _configurations = configurations;
        }

        /// <summary>
        /// Gets the mixer factories availbale for this party.
        /// </summary>
        public ImmutableArray<Factory> Configurations => _configurations;

        /// <summary>
        /// Tries to find a factory with a given <see cref="Factory.Name"/> or returns null.
        /// </summary>
        /// <param name="name">The factory name.</param>
        /// <returns>The factory or null if not found.</returns>
        public Factory? FindFactory( string name )
        {
            Throw.CheckNotNullArgument( name );
            return _configurations.FirstOrDefault( f => f.Name == name );
        }

        /// <summary>
        /// Tries to find the first factory for a mixer that outputs <paramref name="mixerOutputType"/> or returns null.
        /// </summary>
        /// <param name="mixerOutputType">The mixer output type.</param>
        /// <returns>The factory or null if not found.</returns>
        public Factory? FindFactory( Type mixerOutputType )
        {
            Throw.CheckNotNullArgument( mixerOutputType );
            return _configurations.FirstOrDefault( f => mixerOutputType.IsAssignableFrom( f.OutputType ) );
        }

        /// <summary>
        /// Tries to find the factory with a given <see cref="Factory.Name"/> that must output <paramref name="mixerOutputType"/>
        /// or returns null.
        /// </summary>
        /// <param name="mixerOutputType">The required mixer output type.</param>
        /// <param name="name">The required mixer name.</param>
        /// <returns>The factory or null if not found.</returns>
        public Factory? FindFactory( Type mixerOutputType, string name )
        {
            var f = FindFactory( name );
            if( f != null )
            {
                if( !mixerOutputType.IsAssignableFrom( f.OutputType ) ) f = null;
            }
            return f;
        }

        /// <summary>
        /// Find a factory with a given <see cref="Factory.Name"/> or throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="name">The factory name.</param>
        /// <returns>The factory.</returns>
        public Factory FindRequiredFactory( string name )
        {
            var f = FindFactory( name );
            if( f == null )
            {
                Throw.InvalidOperationException( $"No Mixer named '{name}' exist in party '{_party.FullName}'." );
            }
            return f;
        }

        /// <summary>
        /// Find the first factory for a mixer that outputs <paramref name="mixerOutputType"/> or throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="mixerOutputType">The mixer output type.</param>
        /// <returns>The factory.</returns>
        public Factory FindRequiredFactory( Type mixerOutputType )
        {
            var f = FindFactory( mixerOutputType );
            if( f == null )
            {
                Throw.InvalidOperationException( $"No Mixer exist for type '{mixerOutputType.ToCSharpName()}' in party '{_party.FullName}'." );
            }
            return f;
        }

        /// <summary>
        /// Finds the factory with a given <see cref="Factory.Name"/> that must output <paramref name="mixerOutputType"/>
        /// or throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="mixerOutputType">The required mixer output type.</param>
        /// <param name="name">The required mixer name.</param>
        /// <returns>The factory.</returns>
        public Factory FindRequiredFactory( Type mixerOutputType, string name )
        {
            var f = FindRequiredFactory( name );
            if( !mixerOutputType.IsAssignableFrom( f.OutputType ) )
            {
                Throw.InvalidOperationException( $"Mixer named '{name}' in party '{_party.FullName} outputs" +
                                                 $" '{f.OutputType.ToCSharpName()}' required type '{mixerOutputType.ToCSharpName()}' is not compatible." );
            }
            return f;
        }

    }
}
