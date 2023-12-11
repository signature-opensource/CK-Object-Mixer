using CK.AppIdentity;
using CK.Core;
using System;

namespace CK.Object.Mixer
{
    /// <summary>
    /// Scoped service that can create mixers carried by <see cref="IParty"/>.
    /// </summary>
    public class ObjectMixerFactory : IScopedAutoService
    {
        readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new mixer factory.
        /// </summary>
        /// <param name="services"></param>
        public ObjectMixerFactory( IServiceProvider services )
        {
            _services = services;
        }

        /// <summary>
        /// Tries to create a mixer that outputs <typeparamref name="T"/> or returns null.
        /// <para>
        /// The first mixer whose <see cref="ObjectMixerConfiguration.OutputType"/> is compatible with <typeparamref name="T"/>
        /// is selected.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The mixer's output type.</typeparam>
        /// <param name="party">The party that defines the mixer.</param>
        /// <returns>The first compatible typed mixer or null if not found.</returns>
        public IObjectMixer<T>? TryCreate<T>( IParty party ) where T : class
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindFactory( typeof( T ) )?
                        .CreateMixer<T>( _services );
        }

        /// <summary>
        /// Tries to create a mixer from a configuration named <paramref name="name"/> and outputs <typeparamref name="T"/>
        /// or returns null.
        /// </summary>
        /// <typeparam name="T">The mixer's output type.</typeparam>
        /// <param name="party">The party that defines the mixer.</param>
        /// <param name="name">Expected mixer name.</param>
        /// <returns>The typed mixer or null if not found.</returns>
        public IObjectMixer<T>? TryCreate<T>( IParty party, string name ) where T : class
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindFactory( typeof( T ), name )?
                        .CreateMixer<T>( _services );
        }

        /// <summary>
        /// Tries to create an untyped mixer from a configuration named <paramref name="name"/>
        /// or returns null.
        /// </summary>
        /// <param name="party">The party taht defines the mixer.</param>
        /// <param name="name">Expected mixer name.</param>
        /// <returns>The typed mixer.</returns>
        public ObjectMixer? TryCreate( IParty party, string name )
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindRequiredFactory( name )
                        .CreateMixer( _services );
        }


        /// <summary>
        /// Creates a mixer that outputs <typeparamref name="T"/> or throws an <see cref="ArgumentException"/>.
        /// <para>
        /// The first mixer whose <see cref="ObjectMixerConfiguration.OutputType"/> is compatible with <typeparamref name="T"/>
        /// is selected.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The mixer's output type.</typeparam>
        /// <param name="party">The party that defines the mixer.</param>
        /// <returns>The first compatible typed mixer.</returns>
        public IObjectMixer<T> Create<T>( IParty party ) where T : class
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindRequiredFactory( typeof( T ) )
                        .CreateMixer<T>( _services );
        }

        /// <summary>
        /// Creates a mixer from a configuration named <paramref name="name"/> and outputs <typeparamref name="T"/>
        /// or throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <typeparam name="T">The mixer's output type.</typeparam>
        /// <param name="party">The party that defines the mixer.</param>
        /// <param name="name">Expected mixer name.</param>
        /// <returns>The typed mixer.</returns>
        public IObjectMixer<T> Create<T>( IParty party, string name ) where T : class
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindRequiredFactory( typeof( T ), name )
                        .CreateMixer<T>( _services );
        }

        /// <summary>
        /// Creates an untyped mixer from a configuration named <paramref name="name"/>
        /// or throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="party">The party taht defines the mixer.</param>
        /// <param name="name">Expected mixer name.</param>
        /// <returns>The typed mixer.</returns>
        public ObjectMixer Create( IParty party, string name )
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindRequiredFactory( name )
                        .CreateMixer( _services );
        }
    }
}
