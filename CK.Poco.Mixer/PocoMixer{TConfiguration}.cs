using CK.Core;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Root implementation of all mixers.
    /// </summary>
    /// <typeparam name="TConfiguration"></typeparam>
    public abstract class PocoMixer<TConfiguration> : PocoMixer where TConfiguration : PocoMixerConfiguration
    {
        /// <summary>
        /// Initializes a new <see cref="PocoMixer"/>.
        /// </summary>
        /// <param name="configuration">The required configuration.</param>
        protected PocoMixer( TConfiguration configuration )
            : base( configuration )
        {
        }

        /// <inheritdoc />
        public new TConfiguration Configuration => Unsafe.As<TConfiguration>( _configuration );

    }

}
