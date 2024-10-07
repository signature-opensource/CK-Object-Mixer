using CK.AppIdentity;
using CK.Core;
using CK.Object.Mixer;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Mixer.Tests;

[TestFixture]
public class SimpleTests
{
    [Test]
    public async Task configured_processor_and_condition_Async()
    {
        await using var services = await TestHelper.CreateWithApplicationServiceAsync(
            c =>
            {
                c.AddJson( """
                    {
                        "Assemblies": { "CK.Object.Mixer.Tests": "T" },
                        "FullName": "Test/$Test",
                        "Local": {
                            "ObjectMixers": {
                                "Number": {
                                    "Type": "ObjectMixer",
                                    "Processor": { "Type": "NegateDouble, T" },
                                    "OutputCondition": {"Type": "IsGreaterThan, T", "Value": 10 }
                                }
                            }
                        }
                    }
                    """ );
            } );
        await using( var scoped = services.CreateAsyncScope() )
        {
            var appIdentity = scoped.ServiceProvider.GetRequiredService<ApplicationIdentityService>();
            var factory = scoped.ServiceProvider.GetRequiredService<ObjectMixerFactory>();
            var mixer = factory.Create( appIdentity, "Number" );

            // Straight working input.
            {
                var r = await mixer.MixAsync( TestHelper.Monitor, -3712.0 );
                r.Output.Should().Contain( 3712.0 );
                r.Rejected.Should().BeEmpty();
                r.TotalProcessCount.Should().Be( 1 );
            }

            // Working with 2 processes.
            {
                var r = await mixer.MixAsync( TestHelper.Monitor, 42.0 );
                r.Output.Should().Contain( 42.0 );
                r.Rejected.Should().BeEmpty();
                r.TotalProcessCount.Should().Be( 2 );
            }

            // Failing the output condition.
            {
                var rFailed = await mixer.MixAsync( TestHelper.Monitor, 8.0 );
                rFailed.Output.Should().BeEmpty();
                rFailed.Rejected.Should().Contain( (-8.0, "Reached MaxProcessCount: 4.") );
                rFailed.TotalProcessCount.Should().Be( 5 );
            }
            {
                var rFailed = await mixer.MixAsync( TestHelper.Monitor, -8.0 );
                rFailed.Output.Should().BeEmpty();
                rFailed.Rejected.Should().Contain( (8.0, "Reached MaxProcessCount: 4.") );
                rFailed.TotalProcessCount.Should().Be( 5 );
            }
        }

    }

    [Test]
    public async Task StupidString_typed_mixer_Async()
    {
        await using var services = await TestHelper.CreateWithApplicationServiceAsync(
            c =>
            {
                c.AddJson( """
                    {
                        "Assemblies": { "CK.Object.Mixer.Tests": "T" },
                        "FullName": "Test/$Test",
                        "Local": {
                            "ObjectMixers": {
                                "For strings...": {
                                    "Type": "StupidString, T"
                                }
                            }
                        }
                    }
                    """ );
            } );
        await using( var scoped = services.CreateAsyncScope() )
        {
            var appIdentity = scoped.ServiceProvider.GetRequiredService<ApplicationIdentityService>();
            var factory = scoped.ServiceProvider.GetRequiredService<ObjectMixerFactory>();
            var mixer = factory.Create<string>( appIdentity );

            {
                var r = await mixer.MixAsync( TestHelper.Monitor, "abcde" );
                r.Output.Should().Contain( "Processed<abcde>" );
                r.Rejected.Should().BeEmpty();
                r.TotalProcessCount.Should().Be( 1 );
            }
            {
                var r = await mixer.MixAsync( TestHelper.Monitor, 42 );
                r.Output.Concatenate().Should().Contain( "42, int" );
                r.Rejected.Should().BeEmpty();
                r.TotalProcessCount.Should().Be( 1 );
            }
        }
    }

    [Test]
    public async Task StupidString_typed_mixer_with_condition_Async()
    {
        await using var services = await TestHelper.CreateWithApplicationServiceAsync(
            c =>
            {
                c.AddJson( """
                    {
                        "Assemblies": { "CK.Object.Mixer.Tests": "T" },
                        "FullName": "Test/$Test",
                        "Local": {
                            "ObjectMixers": {
                                "For strings...": {
                                    "Type": "StupidString, T",
                                    "EnsureProcessed": true,
                                    "OutputCondition": {
                                        "Type": "Placeholder"
                                    }
                                }
                            }
                        }
                    }
                    """ );
            } );
        await using( var scoped = services.CreateAsyncScope() )
        {
            var appIdentity = scoped.ServiceProvider.GetRequiredService<ApplicationIdentityService>();
            var factory = scoped.ServiceProvider.GetRequiredService<ObjectMixerFactory>();
            var mixer = factory.Create<string>( appIdentity );

            {
                var r = await mixer.MixAsync( TestHelper.Monitor, "abcde" );
                r.Output.Should().Contain( "Processed<abcde>" );
                r.Rejected.Should().BeEmpty();
                r.TotalProcessCount.Should().Be( 1 );
            }
            {
                var r = await mixer.MixAsync( TestHelper.Monitor, 42 );
                r.Output.Concatenate().Should().Contain( "Processed<42>, Processed<int>" );
                r.Rejected.Should().BeEmpty();
                r.TotalProcessCount.Should().Be( 3 );
            }

            var mixerFeature = appIdentity.GetRequiredFeature<ObjectMixerFeature>();
            var f = mixerFeature.FindRequiredFactory( typeof( string ) );
            Throw.DebugAssert( "We have a configured OutputCondition.", f.Current.OutputCondition != null );
            var path = f.Current.OutputCondition.ConfigurationPath + ":<Dynamic>";
            f.TrySetPlaceholder( TestHelper.Monitor, ImmutableConfigurationSection.CreateFromJson( path, """
                {
                  "Type": "All",
                  "Predicates":
                  [
                    { "Type": "Placeholder" },
                    {
                        "Type": "Any",
                        "Predicates":
                        [
                            { "Type": "Placeholder" },
                            {
                                "Type": "Not",
                                "Operand":
                                {
                                    "Type": "IsStringShorterThan, T",
                                    "Length": 14
                                }
                            }
                        ]
                    }
                  ]
                }
                """ ) ).Should().BeTrue();

            var mixer2 = factory.Create<string>( appIdentity );

            {
                var r = await mixer2.MixAsync( TestHelper.Monitor, "abcde" );
                r.Output.Should().Contain( "Processed<abcde>" );
                r.Rejected.Should().BeEmpty();
                r.TotalProcessCount.Should().Be( 1 );
            }
            {
                var r = await mixer2.MixAsync( TestHelper.Monitor, 42 );
                r.Output.Concatenate().Should().Contain( "Processed<int>" );
                r.Rejected.Should().Contain( ("Processed<42>", "Reached MaxProcessCount: 4.") );
                r.TotalProcessCount.Should().Be( 6 );
            }
        }
    }


}
