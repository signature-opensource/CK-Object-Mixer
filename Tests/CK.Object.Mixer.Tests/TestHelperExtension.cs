using CK.AppIdentity;
using CK.Core;
using CK.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Mixer.Tests
{

    static class TestHelperExtension
    {
        public static NormalizedPath TestStoreFolder = TestHelper.TestProjectFolder.AppendPart( "TestStore" );

        public static NormalizedPath GetCleanTestStoreFolder( this IBasicTestHelper helper )
        {
            return helper.CleanupFolder( TestStoreFolder );
        }

        /// <summary>
        /// Creates a minimal <see cref="ServiceProvider"/> with a started ApplicationIdentityService and the
        /// mixer feature and ObjectMixerFactory scoped service.
        /// It must be disposed once done.
        /// </summary>
        /// <param name="this">This test helper.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="configureServices">Optional other service registrations.</param>
        /// <returns>The started service.</returns>
        public static Task<ServiceProvider> CreateWithApplicationServiceAsync( this IBasicTestHelper @this,
                                                                               Action<MutableConfigurationSection> configuration,
                                                                               Action<ServiceCollection>? configureServices = null )
        {
            var c = ApplicationIdentityServiceConfiguration.Create( TestHelper.Monitor, configuration );
            Throw.DebugAssert( c != null );
            return CreateWithApplicationServiceAsync( @this, c, configureServices );
        }

        /// <summary>
        /// Creates a minimal <see cref="ServiceProvider"/> with a started ApplicationIdentityService and the
        /// mixer feature and ObjectMixerFactory scoped service.
        /// It must be disposed once done.
        /// </summary>
        /// <param name="this">This test helper.</param>
        /// <param name="c">The configuration.</param>
        /// <param name="configureServices">Optional other service registrations.</param>
        /// <returns>The started service.</returns>
        public static async Task<ServiceProvider> CreateWithApplicationServiceAsync( this IBasicTestHelper @this,
                                                                                     ApplicationIdentityServiceConfiguration c,
                                                                                     Action<ServiceCollection>? configureServices = null )
        {
            var serviceBuilder = new ServiceCollection();
            serviceBuilder.AddSingleton( c );
            serviceBuilder.AddSingleton<ApplicationIdentityService>();

            serviceBuilder.AddSingleton<ObjectMixerFeatureDriver>();
            serviceBuilder.AddSingleton<IApplicationIdentityFeatureDriver>( sp => sp.GetRequiredService<ObjectMixerFeatureDriver>() );

            serviceBuilder.AddScoped<ObjectMixerFactory>();

            configureServices?.Invoke( serviceBuilder );
            var services = serviceBuilder.BuildServiceProvider();

            var s = services.GetRequiredService<ApplicationIdentityService>();
            await s.StartAndInitializeAsync();
            return services;
        }
    }
}
