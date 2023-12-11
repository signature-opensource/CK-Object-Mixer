using CK.AppIdentity;
using CK.Core;
using System.Collections.Immutable;
using System.Linq;
using System.Security;
using System.Threading.Tasks;

namespace CK.Object.Mixer
{
    /// <summary>
    /// Handles "ObjectMixers" configurations on parties. <see cref="ILocalParty.LocalConfiguration"/> is used
    /// for the <see cref="ApplicationIdentityService"/> and any <see cref="ITenantDomainParty"/>.
    /// </summary>
    public class ObjectMixerFeatureDriver : ApplicationIdentityFeatureDriver
    {
        /// <summary>
        /// Initializes a new <see cref="ObjectMixerFeatureDriver"/>.
        /// </summary>
        /// <param name="s">The identity service.</param>
        public ObjectMixerFeatureDriver( ApplicationIdentityService s )
            : base( s, true )
        {
        }

        /// <summary>
        /// Handles "ObjectMixers" to populate <see cref="ObjectMixerFeature.Configurations"/>.
        /// </summary>
        /// <param name="context">Setup context.</param>
        /// <returns>True on success, false on error.</returns>
        protected override Task<bool> SetupAsync( FeatureLifetimeContext context )
        {
            bool success = true;
            if( IsAllowedFeature( ApplicationIdentityService ) && !Plug( context, ApplicationIdentityService ) )
            {
                success = false;
            }
            foreach( var p in ApplicationIdentityService.AllParties )
            {
                if( IsAllowedFeature( p ) )
                {
                    success &= Plug( context, p );
                }
            }
            return Task.FromResult( success );
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="context">The teardown context.</param>
        /// <returns>The awaitable.</returns>
        protected override Task TeardownAsync( FeatureLifetimeContext context )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles "ObjectMixers" to populate <see cref="ObjectMixerFeature.Configurations"/>.
        /// </summary>
        /// <param name="context">Setup context.</param>
        /// <param name="party">The dynamic party to initialize.</param>
        /// <returns>True on success, false on error.</returns>
        protected override Task<bool> SetupDynamicRemoteAsync( FeatureLifetimeContext context, IOwnedParty party )
        {
            if( IsAllowedFeature( party ) )
            {
                if( !Plug( context, party ) ) return Task.FromResult( false );
            }
            return Task.FromResult( true );
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="context">The teardown context.</param>
        /// <param name="party">The dynamic party to cleanup.</param>
        /// <returns>The awaitable.</returns>
        protected override Task TeardownDynamicRemoteAsync( FeatureLifetimeContext context, IOwnedParty party )
        {
            return Task.CompletedTask;
        }

        static bool Plug( FeatureLifetimeContext context, IParty party )
        {
            // "ObjectMixers" is "local" a feature: if we are on a local party (the root service or
            // a tenant domain), use the "Local" section.
            ApplicationIdentityConfiguration config = party is ILocalParty local
                                                        ? local.LocalConfiguration
                                                        : party.Configuration;
            var c = config.Configuration.TryGetSection( "ObjectMixers" );
            if( c != null )
            {
                if( !c.HasChildren )
                {
                    context.Monitor.Warn( $"ObjectMixers is empty for '{party.FullName}'. Ignoring '{c.Path}'." );
                }
                else
                {
                    var builder = new TypedConfigurationBuilder( config.AssemblyConfiguration );
                    ObjectMixerConfiguration.AddResolver( builder );
                    var mixers = ImmutableArray.CreateBuilder<ObjectMixerFeature.Factory>( c.GetChildren().Count );
                    bool success = true;
                    foreach( var sub in c.GetChildren() )
                    {
                        var m = builder.Create<ObjectMixerConfiguration>( context.Monitor, sub );
                        if( m == null )
                        {
                            success = false;
                        }
                        else
                        {
                            AddMixer( context.Monitor, mixers, sub, m );
                        }
                    }
                    if( !success ) return false;
                    party.AddFeature( new ObjectMixerFeature( party, mixers.ToImmutable() ) );
                }
            }
            return true;
        }

        static void AddMixer( IActivityMonitor monitor,
                              ImmutableArray<ObjectMixerFeature.Factory>.Builder mixers,
                              ImmutableConfigurationSection sub,
                              ObjectMixerConfiguration m )
        {
            int idx = mixers.Count;
            while( idx > 0 )
            {
                var previous = mixers[idx - 1].Initial;
                if( previous.OutputType == m.OutputType )
                {
                    // This is not a warning: this shouldn't be invalid in strict mode: retrieving mixer by "magic string" is
                    // always possible (even if it should be avoided but this is a matter of conception).
                    monitor.Info( $"Mixer named '{m.Name}' cannot be differentiated from '{previous.OutputType}' by its OutputType. Both output '{m.OutputType:C}'. " +
                                  $"It can only be retrieved by specifying its name. (In configuration '{sub.GetParentPath()}'.)" );
                    break;
                }
                if( m.OutputType.IsAssignableFrom( previous.OutputType ) )
                {
                    break;
                }
                --idx;
            }
            // If the mixer has moved, warn: in strict mode we want the configuration to be
            // ordered correctly so that decision order is "readable".
            if( idx < mixers.Count )
            {
                var next = mixers[idx];
                monitor.Warn( $"Mixer named '{m.Name}' that outputs type '{m.OutputType:C}' should appear before '{next.Name}' " +
                              $"(that outputs '{next.Initial.OutputType:C}'. Moving it. (In configuration '{sub.GetParentPath()}'.)" );
            }
            mixers.Insert( idx, new ObjectMixerFeature.Factory( m ) );
        }
    }

}
