using System.Runtime.CompilerServices;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Root implementation of all mixers.
    /// </summary>
    /// <typeparam name="TConfiguration"></typeparam>
    public abstract class BasePocoMixer<TConfiguration> : BasePocoMixer where TConfiguration : PocoMixerConfiguration
    {
        /// <summary>
        /// Initializes a new <see cref="BasePocoMixer"/>.
        /// </summary>
        /// <param name="configuration">The required configuration.</param>
        protected BasePocoMixer( TConfiguration configuration )
            : base( configuration )
        {
        }

        /// <inheritdoc />
        public new TConfiguration Configuration => Unsafe.As<TConfiguration>( _configuration );

    }
}
