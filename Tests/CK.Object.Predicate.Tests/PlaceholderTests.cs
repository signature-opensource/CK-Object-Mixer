using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Predicate.Tests;

[TestFixture]
public class PlaceholderTests
{
    static MutableConfigurationSection GetComplexConfiguration()
    {
        var complexJson = new MutableConfigurationSection( "Root" );
        complexJson.AddJson( """
            {
                // No type resolved to "All" ("And" connector).
                "Predicates": [
                    {
                        // This (stupid) predicate is implemented in this assembly.
                        "Assemblies": {"CK.Object.Predicate.Tests": "Tests"},
                        "Type": "EnumerableMaxCount, Tests",
                        "MaxCount": 5
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
                            {
                                "Type": "Placeholder"
                            }
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
                            {
                                "Type": "Placeholder"
                            }
                        ]
                    },
                ]
            }
            """ );
        return complexJson;
    }

    [Test]
    public void place_holder_replacement()
    {
        var config = new ImmutableConfigurationSection( GetComplexConfiguration() );
        var builder = new TypedConfigurationBuilder();
        ObjectPredicateConfiguration.AddResolver( builder );

        var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( fC != null );
        var f = fC.CreatePredicate();
        Throw.DebugAssert( f != null );

        f( "AxK" ).Should().Be( false, "There must 'A' or 'B' and at least 2 of 'x', 'y', 'z'." );

        // Let's fix this by allowing 'K' to belong to the {'x','y,'z'} set.
        var fix1 = new MutableConfigurationSection( "Root:Predicates:2:Predicates:3", "<Dynamic>" );
        fix1["Type"] = "StringContains, P";
        fix1["Content"] = "K";
        var fC2 = fC.TrySetPlaceholder( TestHelper.Monitor, fix1, out var builderError );
        Throw.DebugAssert( fC2 != null && !builderError && fC2.Synchronous != null );
        var f2 = fC2.Synchronous.CreatePredicate();
        Throw.DebugAssert( f2 != null );

        f2( "AxK" ).Should().Be( true, "Fixed" );
        f2( "xK" ).Should().Be( false, "There must be a 'A' or a 'B'." );

        // Let's remove this constraint by injecting a "AlwaysTrue" in the "Or".
        // Let's fix this by allowing 'K' to belong to the {'x','y,'z'} set.
        var fix2 = new MutableConfigurationSection( "Root:Predicates:1:Predicates:2", "<Dynamic>" );
        fix2["Type"] = "true";
        var fC3 = fC2.TrySetPlaceholder( TestHelper.Monitor, fix2, out builderError );
        Throw.DebugAssert( fC3 != null && !builderError && fC3.Synchronous != null );
        var f3 = fC3.Synchronous.CreatePredicate();
        Throw.DebugAssert( f3 != null );

        f3( "xK" ).Should().Be( true, "Fixed." );
    }

    [Test]
    public async Task place_holder_replacement_Async()
    {
        var config = new ImmutableConfigurationSection( GetComplexConfiguration() );
        var builder = new TypedConfigurationBuilder();
        ObjectAsyncPredicateConfiguration.AddResolver( builder );

        var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
        Throw.DebugAssert( fC != null );
        var f = fC.CreateAsyncPredicate();
        Throw.DebugAssert( f != null );

        (await f( "AxK" )).Should().Be( false, "There must 'A' or 'B' and at least 2 of 'x', 'y', 'z'." );

        // Let's fix this by allowing 'K' to belong to the {'x','y,'z'} set.
        var fix1 = new MutableConfigurationSection( "Root:Predicates:2:Predicates:3", "<Dynamic>" );
        fix1["Type"] = "StringContains, P";
        fix1["Content"] = "K";
        var fC2 = fC.TrySetPlaceholder( TestHelper.Monitor, fix1, out var builderError );
        Throw.DebugAssert( fC2 != null && !builderError );
        var f2 = fC2.CreateAsyncPredicate();
        Throw.DebugAssert( f2 != null );

        (await f2( "AxK" )).Should().Be( true, "Fixed" );
        (await f2( "xK" )).Should().Be( false, "There must be a 'A' or a 'B'." );

        // Let's remove this constraint by injecting a "AlwaysTrue" in the "Or".
        // Let's fix this by allowing 'K' to belong to the {'x','y,'z'} set.
        var fix2 = new MutableConfigurationSection( "Root:Predicates:1:Predicates:2", "<Dynamic>" );
        fix2["Type"] = "true";
        var fC3 = fC2.TrySetPlaceholder( TestHelper.Monitor, fix2, out builderError );
        Throw.DebugAssert( fC3 != null && !builderError );
        var f3 = fC3.CreateAsyncPredicate();
        Throw.DebugAssert( f3 != null );

        (await f3( "xK" )).Should().Be( true, "Fixed." );
    }

}
