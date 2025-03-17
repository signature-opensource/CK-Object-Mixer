using CK.Core;
using Shouldly;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Transform.Tests;

[TestFixture]
public class TransformTests
{
    static MutableConfigurationSection GetConfiguration()
    {
        var c = new MutableConfigurationSection( "Root" );
        c.AddJson( """
            {
                "DefaultAssembly": "CK.Object.Transform.Tests",
                "Transforms": [
                    {
                        "Transforms": [
                            {
                                "Type": "ToString",
                            },
                            {
                                "Type": "AddPrefix",
                                "Prefix": "Before-"
                            },
                            {
                                "Type": "AddSuffix",
                                "Suffix": "-After"
                            }
                        ]
                    },
                    {
                        "Transforms": [
                            {
                                "Type": "AddSuffix",
                                "Suffix": "-OneMore"
                            }
                        ]
                    }
                ]
            }
            """ );
        return c;
    }

    [Test]
    public void basic_tests()
    {
        var builder = new TypedConfigurationBuilder();
        ObjectTransformConfiguration.AddResolver( builder );
        var fC = builder.Create<ObjectTransformConfiguration>( TestHelper.Monitor, GetConfiguration() );
        Throw.DebugAssert( fC != null );
        var f = fC.CreateTransform();
        Throw.DebugAssert( f != null );

        f( "Hello" ).ShouldBe( "Before-Hello-After-OneMore" );
        f( 0 ).ShouldBe( "Before-0-After-OneMore" );
    }

    [Test]
    public async Task basic_tests_Async()
    {
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncTransformConfiguration.AddResolver( builder );

        var fC = builder.Create<ObjectAsyncTransformConfiguration>( TestHelper.Monitor, GetConfiguration() );
        Throw.DebugAssert( fC != null );

        var f = fC.CreateAsyncTransform();
        Throw.DebugAssert( f != null );
        (await f( "Hello" )).ShouldBe( "Before-Hello-After-OneMore" );
        (await f( 0 )).ShouldBe( "Before-0-After-OneMore" );
    }

    static MutableConfigurationSection GetStringOnlyConfiguration()
    {
        var c = new MutableConfigurationSection( "Root" );
        c.AddJson( """
            {
                "DefaultAssembly": "CK.Object.Transform.Tests",
                "Transforms": [
                    {
                        "Transforms": [
                            {
                                "Type": "AddPrefix",
                                "Prefix": "Before-"
                            },
                            {
                                "Type": "AddSuffix",
                                "Suffix": "-After"
                            }
                        ]
                    },
                    {
                        "Transforms": [
                            {
                                "Type": "AddSuffix",
                                "Suffix": "-OneMore"
                            }
                        ]
                    }
                ]
            }
            """ );
        return c;
    }

    [Test]
    public void evaluation_descriptor_finds_error()
    {
        var builder = new TypedConfigurationBuilder();
        ObjectTransformConfiguration.AddResolver( builder );
        var fC = builder.Create<ObjectTransformConfiguration>( TestHelper.Monitor, GetStringOnlyConfiguration() );
        Throw.DebugAssert( fC != null );
        var f = fC.CreateTransform();
        Throw.DebugAssert( f != null );

        f( "Works with a string" ).ShouldBe( "Before-Works with a string-After-OneMore" );

        Util.Invokable( () => f( 0 ) ).ShouldThrow<ArgumentException>();

        var context = new MonitoredTransformDescriptorContext( TestHelper.Monitor );
        var fH = fC.CreateDescriptor( context );
        Throw.DebugAssert( fH != null );

        fH.TransformSync( "Works with a string" ).ShouldBe( "Before-Works with a string-After-OneMore" );

        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            var r = fH.TransformSync( 0 );
            r.ShouldBeAssignableTo<ArgumentException>();
            ((ArgumentException)r).Message.ShouldBe( "String expected, got 'int'." );

            logs.ShouldContain( "Transform 'Root:Transforms:0:Transforms:0' error while processing:" );
        }

    }

    [Test]
    public async Task evaluation_descriptor_finds_error_Async()
    {
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncTransformConfiguration.AddResolver( builder );
        var fC = builder.Create<ObjectAsyncTransformConfiguration>( TestHelper.Monitor, GetStringOnlyConfiguration() );
        Throw.DebugAssert( fC != null );
        var f = fC.CreateAsyncTransform();
        Throw.DebugAssert( f != null );

        (await f( "Works with a string" )).ShouldBe( "Before-Works with a string-After-OneMore" );

        await Util.Awaitable( async () => await f( 0 ) ).ShouldThrowAsync<ArgumentException>();

        var context = new MonitoredTransformDescriptorContext( TestHelper.Monitor );
        var fH = fC.CreateDescriptor( context );
        Throw.DebugAssert( fH != null );

        (await fH.TransformAsync( "Works with a string" )).ShouldBe( "Before-Works with a string-After-OneMore" );

        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            var r = await fH.TransformAsync( 0 );
            r.ShouldBeAssignableTo<ArgumentException>();
            ((ArgumentException)r).Message.ShouldBe( "String expected, got 'int'." );

            logs.ShouldContain( "Transform 'Root:Transforms:0:Transforms:0' error while processing:" );
        }

    }


}

