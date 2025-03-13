using CK.Core;
using Shouldly;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Predicate.Tests;


[TestFixture]
public class PredicateTests
{
    [Test]
    public void empty_configuration_fails()
    {
        var config = new MutableConfigurationSection( "Root" );

        var builder = new TypedConfigurationBuilder();
        ObjectPredicateConfiguration.AddSynchronousOnlyResolver( builder );
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
            fC.ShouldBeNull();
            logs.ShouldContain( "Configuration 'Root' must have children to be considered a default 'GroupPredicateConfiguration'." );
        }
    }

    [TestCase( true )]
    [TestCase( false )]
    public async Task type_can_be_true_or_false_Async( bool always )
    {
        var config = new MutableConfigurationSection( "Root" );
        config["Condition"] = always.ToString();
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        {
            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
            Throw.DebugAssert( fC != null );
            var f = fC.CreatePredicate();
            Throw.DebugAssert( f != null );
            f( this ).ShouldBe( always );
        }
        {
            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
            Throw.DebugAssert( fC != null );
            var f = fC.CreateAsyncPredicate();
            Throw.DebugAssert( f != null );
            (await f( this )).ShouldBe( always );
        }
    }

    [TestCase( "All" )]
    [TestCase( "Any" )]
    public void type_can_be_All_or_Any( string t )
    {
        var config = new MutableConfigurationSection( "Root" );
        config["Condition"] = t;
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        {
            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
            Throw.DebugAssert( fC != null );

            fC.ShouldBeAssignableTo<IGroupPredicateConfiguration>();
            ((IGroupPredicateConfiguration)fC).Predicates.ShouldBeEmpty();
            ((IGroupPredicateConfiguration)fC).All.ShouldBe( t == "All" );
            ((IGroupPredicateConfiguration)fC).Any.ShouldBe( t == "Any" );
            ((IGroupPredicateConfiguration)fC).AtLeast.ShouldBe( t == "All" ? 0 : 1 );
        }
        {
            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
            Throw.DebugAssert( fC != null );

            fC.ShouldBeAssignableTo<IGroupPredicateConfiguration>();
            ((IGroupPredicateConfiguration)fC).Predicates.ShouldBeEmpty();
            ((IGroupPredicateConfiguration)fC).All.ShouldBe( t == "All" );
            ((IGroupPredicateConfiguration)fC).Any.ShouldBe( t == "Any" );
            ((IGroupPredicateConfiguration)fC).AtLeast.ShouldBe( t == "All" ? 0 : 1 );
        }
    }

    [Test]
    public async Task default_group_is_All_Async()
    {
        var config = new MutableConfigurationSection( "Root" );
        config["Conditions:0:Type"] = "true";
        var builder = new TypedConfigurationBuilder();
        // Relaces default "Predicates" by "Conditions".
        ObjectAsyncPredicateConfiguration.AddResolver( builder, compositeItemsFieldName: "Conditions" );

        // Sync
        {
            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            fC.ShouldBeAssignableTo<IGroupPredicateConfiguration>();
            ((IGroupPredicateConfiguration)fC).Predicates.Count.ShouldBe( 1 );
            ((IGroupPredicateConfiguration)fC).All.ShouldBeTrue();
            ((IGroupPredicateConfiguration)fC).Any.ShouldBeFalse();
            ((IGroupPredicateConfiguration)fC).AtLeast.ShouldBe( 0 );
            ((IGroupPredicateConfiguration)fC).Predicates[0].ShouldBeAssignableTo<AlwaysTruePredicateConfiguration>();

            fC.GetType().Name.ShouldBe( "GroupPredicateConfiguration", "Synchronous group." );

            var f = fC.CreatePredicate();
            Throw.DebugAssert( f != null );
            f( this ).ShouldBeTrue();
        }
        // Async
        {
            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            fC.ShouldBeAssignableTo<IGroupPredicateConfiguration>();
            ((IGroupPredicateConfiguration)fC).Predicates.Count.ShouldBe( 1 );
            ((IGroupPredicateConfiguration)fC).All.ShouldBeTrue();
            ((IGroupPredicateConfiguration)fC).Any.ShouldBeFalse();
            ((IGroupPredicateConfiguration)fC).AtLeast.ShouldBe( 0 );
            ((IGroupPredicateConfiguration)fC).Predicates[0].ShouldBeAssignableTo<AlwaysTruePredicateConfiguration>();

            fC.GetType().Name.ShouldBe( "GroupPredicateConfiguration", "Also the synchronous group because its predicate is sync." );

            var f = fC.CreateAsyncPredicate();
            Throw.DebugAssert( f != null );
            (await f( this )).ShouldBeTrue();
        }
    }

    static MutableConfigurationSection GetComplexConfiguration()
    {
        var complexJson = new MutableConfigurationSection( "Root" );
        complexJson.AddJson( """
            {
                // No type resolved to "All" ("And" connector).
                "Predicates": [
                    {
                        "Predicates": [
                            {
                                // This is the same as below.
                                "Type": true,
                            },
                            {
                                // An intrinsic AlwaysTrue object predicate.
                                // A "AlwaysFalse" is also available.
                                "Type": "AlwaysTrue",
                            },
                            {
                                // This (stupid) predicate is implemented in this assembly.
                                "Assemblies": {"CK.Object.Predicate.Tests": "Tests"},
                                "Type": "EnumerableMaxCount, Tests",
                                "MaxCount": 5
                            }
                        ]
                    },
                    {
                        // "Any" is the "Or" connector.
                        "Type": "Any",
                        "Assemblies": {"CK.Object.Predicate.Tests": "P"},
                        "Predicates": [
                            {
                                "Type": "StringContains, P",
                                "Content": "A"
                            },
                            {
                                "Type": "StringContains, P",
                                "Content": "B"
                            },
                        ]
                    },
                    {
                        // The "Group" with "AtLeast" enables a "n among m" condition.
                        "Type": "Group",
                        "AtLeast": 2,
                        "Assemblies": {"CK.Object.Predicate.Tests": "P"},
                        "Predicates": [
                            {
                                "Type": "StringContains, P",
                                "Content": "x"
                            },
                            {
                                "Type": "StringContains, P",
                                "Content": "y"
                            },
                            {
                                "Type": "StringContains, P",
                                "Content": "z"
                            },
                        ]
                    },
                ]
            }
            """ );
        return complexJson;
    }

    [Test]
    public void complex_configuration_tree()
    {
        MutableConfigurationSection config = GetComplexConfiguration();
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( fC != null );

        var f = fC.CreatePredicate();
        Throw.DebugAssert( f != null );
        f( 0 ).ShouldBe( false );
        f( "Ax" ).ShouldBe( false );
        f( "Axy" ).ShouldBe( true );
        f( "Bzy" ).ShouldBe( true );
        f( "Bzy but too long" ).ShouldBe( false );
    }

    [Test]
    public async Task complex_configuration_tree_Async()
    {
        MutableConfigurationSection config = GetComplexConfiguration();
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( fC != null );

        var f = fC.CreateAsyncPredicate();
        Throw.DebugAssert( f != null );
        (await f( 0 )).ShouldBe( false );
        (await f( "Ax" )).ShouldBe( false );
        (await f( "Axy" )).ShouldBe( true );
        (await f( "Bzy" )).ShouldBe( true );
        (await f( "Bzy but too long" )).ShouldBe( false );
    }

    [Test]
    public void complex_configuration_tree_with_Descriptor()
    {
        MutableConfigurationSection config = GetComplexConfiguration();
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( fC != null );

        var context = new MonitoredPredicateDescriptorContext( TestHelper.Monitor );

        var f = fC.CreateDescriptor( context );
        Throw.DebugAssert( f != null );
        f.EvaluateSync( 0 ).ShouldBe( false );
        f.EvaluateSync( "Ax" ).ShouldBe( false );
        f.EvaluateSync( "Axy" ).ShouldBe( true );
        f.EvaluateSync( "Bzy" ).ShouldBe( true );
        f.EvaluateSync( "Bzy but too long" ).ShouldBe( false );
    }

    [Test]
    public async Task complex_configuration_tree_with_Descriptor_Async()
    {
        MutableConfigurationSection config = GetComplexConfiguration();
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( fC != null );

        var context = new MonitoredPredicateDescriptorContext( TestHelper.Monitor );

        var f = fC.CreateDescriptor( context );
        Throw.DebugAssert( f != null );
        (await f.EvaluateAsync( 0 )).ShouldBe( false );
        (await f.EvaluateAsync( "Ax" )).ShouldBe( false );
        (await f.EvaluateAsync( "Axy" )).ShouldBe( true );
        (await f.EvaluateAsync( "Bzy" )).ShouldBe( true );
        (await f.EvaluateAsync( "Bzy but too long" )).ShouldBe( false );
    }

    [Test]
    public async Task Not_predicate_test_Async()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "Type": "Not",
                "Operand":
                {
                    "Assemblies": { "CK.Object.Predicate.Tests": "P"},
                    "Type": "StringContains, P",
                    "Content": "Hello!"
                }
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        {
            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            var f = fC.CreatePredicate();
            Throw.DebugAssert( f != null );
            f( "With Hello! fails." ).ShouldBeFalse();
            f( "Without succeeds." ).ShouldBeTrue();
        }
        {
            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            var f = fC.CreateAsyncPredicate();
            Throw.DebugAssert( f != null );
            (await f( "With Hello! fails." )).ShouldBeFalse();
            (await f( "Without succeeds." )).ShouldBeTrue();
        }
    }

    [Test]
    public async Task IsType_helper_test_Async()
    {
        var config = ImmutableConfigurationSection.CreateFromJson( "Root",
            """
            {
                "Assemblies": { "CK.Object.Predicate.Tests": "Test"},
                "Type": "Any",
                "Predicates": [
                    {
                        "Type": "IsString, Test",
                    },
                    {
                        "Type": "IsDouble, Test",
                    }
                ]
            }
            """ );
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        // Sync
        {
            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            var f = fC.CreatePredicate();
            Throw.DebugAssert( f != null );
            f( 0 ).ShouldBeFalse();
            f( this ).ShouldBeFalse();
            f( 0.0 ).ShouldBeTrue();
            f( "Hello" ).ShouldBeTrue();
        }
        // Async
        {
            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );
            var f = fC.CreateAsyncPredicate();
            Throw.DebugAssert( f != null );
            (await f( this )).ShouldBeFalse();
            (await f( 0 )).ShouldBeFalse();
            (await f( double.Epsilon )).ShouldBeTrue();
            (await f( string.Empty )).ShouldBeTrue();
        }
    }

}
