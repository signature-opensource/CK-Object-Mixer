using CK.AppIdentity;
using CK.Core;
using System;

namespace CK.Object.Mixer
{
    public class ObjectMixerFactory : IScopedAutoService
    {
        readonly IServiceProvider _services;

        public ObjectMixerFactory( IServiceProvider services )
        {
            _services = services;
        }

        public IObjectMixer<T> Create<T>( IParty party ) where T : class
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindRequiredFactory( typeof( T ) )
                        .CreateMixer<T>( _services );
        }

        public IObjectMixer<T> Create<T>( IParty party, string name ) where T : class
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindRequiredFactory( typeof( T ), name )
                        .CreateMixer<T>( _services );
        }

        public ObjectMixer Create( IParty party, string name )
        {
            return party.GetRequiredFeature<ObjectMixerFeature>()
                        .FindRequiredFactory( name )
                        .CreateMixer( _services );
        }
    }
}
