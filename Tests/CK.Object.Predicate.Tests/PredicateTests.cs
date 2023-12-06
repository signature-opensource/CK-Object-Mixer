using CK.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Predicate.Tests
{

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
                fC.Should().BeNull();
                logs.Should().Contain( "Configuration 'Root' must have children to be considered a default 'GroupPredicateConfiguration'." );
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
                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                f( this ).Should().Be( always );
            }
            {
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
                Throw.DebugAssert( fC != null );
                var f = fC.CreateAsyncPredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                (await f( this )).Should().Be( always );
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

                fC.Should().BeAssignableTo<IGroupPredicateConfiguration>();
                ((IGroupPredicateConfiguration)fC).Predicates.Should().BeEmpty();
                ((IGroupPredicateConfiguration)fC).All.Should().Be( t == "All" );
                ((IGroupPredicateConfiguration)fC).Any.Should().Be( t == "Any" );
                ((IGroupPredicateConfiguration)fC).AtLeast.Should().Be( t == "All" ? 0 : 1 );
            }
            {
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config.GetRequiredSection( "Condition" ) );
                Throw.DebugAssert( fC != null );

                fC.Should().BeAssignableTo<IGroupPredicateConfiguration>();
                ((IGroupPredicateConfiguration)fC).Predicates.Should().BeEmpty();
                ((IGroupPredicateConfiguration)fC).All.Should().Be( t == "All" );
                ((IGroupPredicateConfiguration)fC).Any.Should().Be( t == "Any" );
                ((IGroupPredicateConfiguration)fC).AtLeast.Should().Be( t == "All" ? 0 : 1 );
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
                fC.Should().BeAssignableTo<IGroupPredicateConfiguration>();
                ((IGroupPredicateConfiguration)fC).Predicates.Should().HaveCount( 1 );
                ((IGroupPredicateConfiguration)fC).All.Should().BeTrue();
                ((IGroupPredicateConfiguration)fC).Any.Should().BeFalse();
                ((IGroupPredicateConfiguration)fC).AtLeast.Should().Be( 0 );
                ((IGroupPredicateConfiguration)fC).Predicates[0].Should().BeAssignableTo<AlwaysTruePredicateConfiguration>();

                fC.GetType().Name.Should().Be( "GroupPredicateConfiguration", "Synchronous group." );

                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                f( this ).Should().BeTrue();
            }
            // Async
            {
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                fC.Should().BeAssignableTo<IGroupPredicateConfiguration>();
                ((IGroupPredicateConfiguration)fC).Predicates.Should().HaveCount( 1 );
                ((IGroupPredicateConfiguration)fC).All.Should().BeTrue();
                ((IGroupPredicateConfiguration)fC).Any.Should().BeFalse();
                ((IGroupPredicateConfiguration)fC).AtLeast.Should().Be( 0 );
                ((IGroupPredicateConfiguration)fC).Predicates[0].Should().BeAssignableTo<AlwaysTruePredicateConfiguration>();

                fC.GetType().Name.Should().Be( "GroupPredicateConfiguration", "Also the synchronous group because its predicate is sync." );

                var f = fC.CreateAsyncPredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                (await f( this )).Should().BeTrue();
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

            var f = fC.CreatePredicate( TestHelper.Monitor );
            Throw.DebugAssert( f != null );
            f( 0 ).Should().Be( false );
            f( "Ax" ).Should().Be( false );
            f( "Axy" ).Should().Be( true );
            f( "Bzy" ).Should().Be( true );
            f( "Bzy but too long" ).Should().Be( false );
        }

        [Test]
        public async Task complex_configuration_tree_Async()
        {
            MutableConfigurationSection config = GetComplexConfiguration();
            var builder = new TypedConfigurationBuilder();
            ObjectAsyncPredicateConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );

            var f = fC.CreateAsyncPredicate( TestHelper.Monitor );
            Throw.DebugAssert( f != null );
            (await f( 0 )).Should().Be( false );
            (await f( "Ax" )).Should().Be( false );
            (await f( "Axy" )).Should().Be( true );
            (await f( "Bzy" )).Should().Be( true );
            (await f( "Bzy but too long" )).Should().Be( false );
        }

        [Test]
        public void complex_configuration_tree_with_EvaluationHook()
        {
            MutableConfigurationSection config = GetComplexConfiguration();
            var builder = new TypedConfigurationBuilder();
            ObjectAsyncPredicateConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );

            var hook = new MonitoredPredicateHookContext( TestHelper.Monitor );

            var f = fC.CreateHook( TestHelper.Monitor, hook );
            Throw.DebugAssert( f != null );
            f.Evaluate( 0 ).Should().Be( false );
            f.Evaluate( "Ax" ).Should().Be( false );
            f.Evaluate( "Axy" ).Should().Be( true );
            f.Evaluate( "Bzy" ).Should().Be( true );
            f.Evaluate( "Bzy but too long" ).Should().Be( false );
        }

        [Test]
        public async Task complex_configuration_tree_with_EvaluationHook_Async()
        {
            MutableConfigurationSection config = GetComplexConfiguration();
            var builder = new TypedConfigurationBuilder();
            ObjectAsyncPredicateConfiguration.AddResolver( builder );

            var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
            Throw.DebugAssert( fC != null );

            var hook = new MonitoredPredicateHookContext( TestHelper.Monitor );

            var f = fC.CreateAsyncHook( TestHelper.Monitor, hook );
            Throw.DebugAssert( f != null );
            (await f.EvaluateAsync( 0 )).Should().Be( false );
            (await f.EvaluateAsync( "Ax" )).Should().Be( false );
            (await f.EvaluateAsync( "Axy" )).Should().Be( true );
            (await f.EvaluateAsync( "Bzy" )).Should().Be( true );
            (await f.EvaluateAsync( "Bzy but too long" )).Should().Be( false );
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
                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                f( "With Hello! fails." ).Should().BeFalse();
                f( "Without succeeds." ).Should().BeTrue();
            }
            {
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                var f = fC.CreateAsyncPredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                (await f( "With Hello! fails." )).Should().BeFalse();
                (await f( "Without succeeds." )).Should().BeTrue();
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
                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                f( 0 ).Should().BeFalse();
                f( this ).Should().BeFalse();
                f( 0.0 ).Should().BeTrue();
                f( "Hello" ).Should().BeTrue();
            }
            // Async
            {
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                var f = fC.CreateAsyncPredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                (await f( this )).Should().BeFalse();
                (await f( 0 )).Should().BeFalse();
                (await f( double.Epsilon )).Should().BeTrue();
                (await f( string.Empty )).Should().BeTrue();
            }
        }

    }
}
