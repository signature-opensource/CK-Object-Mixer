using CK.Core;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Mixer
{
    /// <summary>
    /// Root implementation of all mixers.
    /// </summary>
    /// <typeparam name="TConfiguration"></typeparam>
    public abstract class BaseObjectMixer<TConfiguration> : BaseObjectMixer
        where TConfiguration : ObjectMixerConfiguration
    {
        /// <summary>
        /// Initializes a new <see cref="BaseObjectMixer"/>.
        /// </summary>
        /// <param name="configuration">The required configuration.</param>
        protected BaseObjectMixer( TConfiguration configuration )
            : base( configuration )
        {
        }

        /// <inheritdoc />
        public new TConfiguration Configuration => Unsafe.As<TConfiguration>( base.Configuration );

    }
}
