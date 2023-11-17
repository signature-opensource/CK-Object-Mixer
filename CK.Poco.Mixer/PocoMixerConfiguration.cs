using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Mixer configuration base class.
    /// </summary>
    public abstract class PocoMixerConfiguration
    {
        readonly ImmutableConfigurationSection _configuration;

        /// <summary>
        /// Captures the configuration section. The monitor and builder are unused
        /// at this level but this is the standard signature that all configuration
        /// must support.
        /// </summary>
        /// <param name="monitor">The monitor that signals errors or warnings.</param>
        /// <param name="builder">The builder.</param>
        /// <param name="configuration">The configuration serction.</param>
        protected PocoMixerConfiguration( IActivityMonitor monitor,
                                          PolymorphicConfigurationTypeBuilder builder,
                                          ImmutableConfigurationSection configuration )
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration section.
        /// </summary>
        public ImmutableConfigurationSection Configuration => _configuration;

        /// <summary>
        /// Creates a mixer. A null return is not necessarily an error (errors
        /// should be handled via the monitor - see <see cref="ActivityMonitorExtension.OnError(IActivityMonitor, Action)"/>
        /// for instance). A null return must be ignored: the configuration is "disabled", "non applicable", or is a "placeholder".
        /// </summary>
        /// <param name="monitor">The monitor that must be used to signal errors.</param>
        /// <returns>A strategy or null on error or if, for any reason, no strategy must be created from this configuration.</returns>
        public abstract PocoMixer? CreateMixer( IActivityMonitor monitor, IServiceProvider services );

    }
}
