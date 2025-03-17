using CK.Core;
using CK.Object.Predicate;
using CK.Object.Transform;
using Shouldly;
using Microsoft.VisualBasic;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Processor.Tests;

[TestFixture]
public class BasicTests
{
    [Test]
    public async Task basic_test_Async()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "Assemblies": { "CK.Object.Processor.Tests": "Test"},
                "Processors": [
                    {
                        "Type": "ToUpperCase, Test",
                    },
                    {
                        "Type": "NegateDouble, Test",
                    },
                    {
                        "Type": "AddRandomFriendToUser, Test",
                        "MinAge": 40
                    }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        ObjectProcessorConfiguration.AddResolver( builder );

        var services = new SimpleServiceContainer();
        services.Add( new UserService() );

        // Sync
        {
            var fC = builder.Create<ObjectProcessorConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            // Function
            {
                var f = fC.CreateProcessor( services );
                Throw.DebugAssert( f != null );
                f( 3712 ).ShouldBeNull();
                f( this ).ShouldBeNull();
                f( 3712.5 ).ShouldBe( -3712.5 );
                f( "Hello!" ).ShouldBe( "HELLO!" );

                var u = new UserRecord( "Zoe", 44, new List<UserRecord>() );
                var uO = f( u );
                uO.ShouldBeSameAs( u );
                u = (UserRecord)uO!;
                u.Friends.Count.ShouldBe( 1 );
            }
            // Descriptor
            var context = new MonitoredProcessorDescriptorContext( TestHelper.Monitor );
            {
                var fH = fC.CreateDescriptor( context, services );
                Throw.DebugAssert( fH != null );
                fH.SyncProcess( 3712 ).ShouldBeNull();
                fH.SyncProcess( this ).ShouldBeNull();
                fH.SyncProcess( 3712.5 ).ShouldBe( -3712.5 );
                fH.SyncProcess( "Hello!" ).ShouldBe( "HELLO!" );

                var u = new UserRecord( "Zoe", 44, new List<UserRecord>() );
                var uO = fH.SyncProcess( u );
                uO.ShouldBeSameAs( u );
                u = (UserRecord)uO!;
                u.Friends.Count.ShouldBe( 1 );
            }
        }
        // Async
        {
            var fC = builder.Create<ObjectProcessorConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            // Function
            {
                var f = fC.CreateAsyncProcessor( services );
                Throw.DebugAssert( f != null );
                (await f( 3712 )).ShouldBeNull();
                (await f( this )).ShouldBeNull();
                (await f( 3712.5 )).ShouldBe( -3712.5 );
                (await f( "Hello!" )).ShouldBe( "HELLO!" );

                var u = new UserRecord( "Zoe", 44, new List<UserRecord>() );
                var uO = await f( u );
                uO.ShouldBeSameAs( u );
                u = (UserRecord)uO!;
                u.Friends.Count.ShouldBe( 1 );
            }
            // Descriptor
            var context = new MonitoredProcessorDescriptorContext( TestHelper.Monitor );
            {
                var fH = fC.CreateDescriptor( context, services );
                Throw.DebugAssert( fH != null );
                (await fH.ProcessAsync( 3712 )).ShouldBeNull();
                (await fH.ProcessAsync( this )).ShouldBeNull();
                (await fH.ProcessAsync( 3712.5 )).ShouldBe( -3712.5 );
                (await fH.ProcessAsync( "Hello!" )).ShouldBe( "HELLO!" );

                var u = new UserRecord( "Zoe", 44, new List<UserRecord>() );
                var uO = await fH.ProcessAsync( u );
                uO.ShouldBeSameAs( u );
                u = (UserRecord)uO!;
                u.Friends.Count.ShouldBe( 1 );
            }
        }
    }

    [Test]
    public async Task basic_with_conditions_and_final_transform_Async()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "Assemblies": { "CK.Object.Processor.Tests": "Test"},
                "Processors": [
                    {
                        "Condition":
                        {
                            "Type": "IsStringLongerThan, Test",
                            "Length": 10
                        },
                        "Type": "ToUpperCase, Test",
                    },
                    {
                        "Condition":
                        {
                            "Type": "IsStringShorterThan, Test",
                            "Length": 6
                        },
                        "Type": "ToUpperCase, Test",
                    },
                    {
                        "Type": "NegateDouble, Test",
                    }
                ],
                "Transform":
                {
                    "Type": "ToString, Test"
                }
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        ObjectProcessorConfiguration.AddResolver( builder );

        // Sync
        {
            var fC = builder.Create<ObjectProcessorConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            // Function
            {
                var f = fC.CreateProcessor();
                Throw.DebugAssert( f != null );
                f( 3712 ).ShouldBeNull();
                f( this ).ShouldBeNull();
                f( 3712.5 ).ShouldBe( "-3712.5" );

                f( "Hello!" ).ShouldBeNull( "Not processed (length is 6)." );
                f( "Hello world!" ).ShouldBe( "HELLO WORLD!" );
                f( "Hell!" ).ShouldBe( "HELL!" );
            }

            // Descriptor
            var context = new MonitoredProcessorDescriptorContext( TestHelper.Monitor );
            {
                var fH = fC.CreateDescriptor( context );
                Throw.DebugAssert( fH != null );
                fH.SyncProcess( 3712 ).ShouldBeNull();
                fH.SyncProcess( this ).ShouldBeNull();
                fH.SyncProcess( 3712.5 ).ShouldBe( "-3712.5" );
                fH.SyncProcess( "Hello!" ).ShouldBeNull( "Not processed (length is 6)." );
                fH.SyncProcess( "Hello world!" ).ShouldBe( "HELLO WORLD!" );
                fH.SyncProcess( "Hell!" ).ShouldBe( "HELL!" );
            }

        }
        // Async
        {
            var fC = builder.Create<ObjectProcessorConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            // Function
            {
                var f = fC.CreateAsyncProcessor();
                Throw.DebugAssert( f != null );
                (await f( 3712 )).ShouldBeNull();
                (await f( this )).ShouldBeNull();
                (await f( 3712.5 )).ShouldBe( "-3712.5" );
                (await f( "Hello!" )).ShouldBeNull( "Not processed (length is 6)." );
                (await f( "Hello world!" )).ShouldBe( "HELLO WORLD!" );
                (await f( "Hell!" )).ShouldBe( "HELL!" );
            }

            // Descriptor
            var context = new MonitoredProcessorDescriptorContext( TestHelper.Monitor );
            {
                var fH = fC.CreateDescriptor( context );
                Throw.DebugAssert( fH != null );
                (await fH.ProcessAsync( 3712 )).ShouldBeNull();
                (await fH.ProcessAsync( this )).ShouldBeNull();
                (await fH.ProcessAsync( 3712.5 )).ShouldBe( "-3712.5" );
                (await fH.ProcessAsync( "Hello!" )).ShouldBeNull( "Not processed (length is 6)." );
                (await fH.ProcessAsync( "Hello world!" )).ShouldBe( "HELLO WORLD!" );
                (await fH.ProcessAsync( "Hell!" )).ShouldBe( "HELL!" );
            }
        }
    }
}
