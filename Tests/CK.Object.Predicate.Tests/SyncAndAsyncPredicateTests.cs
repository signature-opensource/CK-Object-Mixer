using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.Object.Predicate.Tests
{
    [TestFixture]
    public class SyncAndAsyncPredicateTests
    {
        static NormalizedPath ThisFile => TestHelper.TestProjectFolder.AppendPart( "SyncAndAsyncPredicateTests.cs" ); 

        [Test]
        public async Task sync_or_async_predicate_Async()
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                $$"""
                {
                    "Assemblies": { "CK.Object.Predicate.Tests": "Test"},
                    "Type": "All",
                    "Predicates": [
                        {
                            // This one will be the Async (if Async resolver has been registered AND an async predicate is resolved),
                            // or the Sync (if only sync resolver has been registered OR a sync predicate is resolved).
                            "Type": "IsInTextFile, Test",
                            "FileName": "{{ThisFile}}"
                        },
                        {
                            // This one will always be the Sync one.
                            "Type": "IsInTextFilePredicate, Test",
                            "FileName": "{{ThisFile}}"
                        }
                    ]
                }
                """ );

            // Using AddSynchronousOnlyResolver: no Async can be resolved.
            {
                var builder = new TypedConfigurationBuilder();
                ObjectPredicateConfiguration.AddSynchronousOnlyResolver( builder );

                using( TestHelper.Monitor.CollectTexts( out var logs ) )
                {
                    var c = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
                    c.Should().BeNull();
                    logs.Should().Contain( "Unable to find a resolver for 'ObjectAsyncPredicateConfiguration' (Registered resolvers Base Types are: 'CK.Object.Predicate.ObjectPredicateConfiguration')." );
                }
                var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                f( "This one will always be the Sync one" ).Should().BeTrue();
                f( "NOT" + "HERE!" ).Should().BeFalse();
            }
            // Using regular Resolver registration.
            // When resolving a sync predicate, the sync version has been selected.
            {
                var builder = new TypedConfigurationBuilder();
                ObjectAsyncPredicateConfiguration.AddResolver( builder );
                // The Sync can be resolved.
                var fC = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                var f = fC.CreatePredicate( TestHelper.Monitor );
                Throw.DebugAssert( f != null );
                f( "This one will always be the Sync one" ).Should().BeTrue();
                f( "NOT" + "HERE!" ).Should().BeFalse();
            }
            // When resolving a async predicate, the async version has been selected for "IsInTextFile"
            // but the second "IsInTextFilePredicate" is the sync one.
            {
                var builder = new TypedConfigurationBuilder();
                ObjectAsyncPredicateConfiguration.AddResolver( builder );

                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                var f = fC.CreateAsyncPredicate();
                Throw.DebugAssert( f != null );
                (await f( "This one will always be the Sync one" )).Should().BeTrue();
                (await f( "NOT" + "HERE!" )).Should().BeFalse();

                // The root cannot be synchronous.
                fC.Synchronous.Should().BeNull();
                // The first "IsInTextFile" resolved to "IsInTextFileAsyncPredicate".
                // The second explicitly "IsInTextFilePredicate" is forced to be sync.
                var g = (IGroupPredicateConfiguration)fC;
                g.Predicates[0].GetType().Name.Should().Be( "IsInTextFileAsyncPredicateConfiguration" );
                g.Predicates[1].GetType().Name.Should().Be( "IsInTextFilePredicateConfiguration" );
            }
        }

        [Test]
        public async Task forcing_async_predicate_Async()
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root",
                $$"""
                {
                    "Assemblies": { "CK.Object.Predicate.Tests": "Test"},
                    "Type": "Any",
                    "Predicates": [
                        {
                            // This one will always be the Async one.
                            // If only sync resolver has been registered, it is an error.
                            "Type": "IsInTextFileAsyncPredicate, Test",
                            "FileName": "{{ThisFile}}"
                        },
                    ]
                }
                """ );
            // Using AddSynchronousOnlyResolver: no Async can be resolved
            // and because the predicate is explicitly async, sync predicates cannnot be resolved either.
            // => A configuration that has an Async predicate MUST use ObjectAsyncPredicateConfiguration.AddResolver!
            {
                var builder = new TypedConfigurationBuilder();
                ObjectPredicateConfiguration.AddSynchronousOnlyResolver( builder );

                using( TestHelper.Monitor.CollectTexts( out var logs ) )
                {
                    var c = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
                    c.Should().BeNull();
                    logs.Should().Contain( "Unable to find a resolver for 'ObjectAsyncPredicateConfiguration' (Registered resolvers Base Types are: 'CK.Object.Predicate.ObjectPredicateConfiguration')." );
                }
                using( TestHelper.Monitor.CollectTexts( out var logs ) )
                {
                    var c = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
                    c.Should().BeNull();
                    logs.Should().Contain( "The 'IsInTextFileAsyncPredicate, Test' type name resolved to 'CK.Object.Predicate.IsInTextFileAsyncPredicateConfiguration' but this type is not compatible with 'CK.Object.Predicate.ObjectPredicateConfiguration'. (Configuration 'Root:Predicates:0:Type'.)" );
                }
            }
            // Using ObjectAsyncPredicateConfiguration.AddResolver:
            //  - No sync predicate can be resolved.
            //  - Async works.
            {
                var builder = new TypedConfigurationBuilder();
                ObjectAsyncPredicateConfiguration.AddResolver( builder );

                using( TestHelper.Monitor.CollectTexts( out var logs ) )
                {
                    var c = builder.Create<ObjectPredicateConfiguration>( TestHelper.Monitor, config );
                    c.Should().BeNull();
                    logs.Should().Contain( "The 'IsInTextFileAsyncPredicate, Test' type name resolved to 'CK.Object.Predicate.IsInTextFileAsyncPredicateConfiguration' but this type is not compatible with 'CK.Object.Predicate.ObjectPredicateConfiguration'. (Configuration 'Root:Predicates:0:Type'.)" );
                }
                var fC = builder.Create<ObjectAsyncPredicateConfiguration>( TestHelper.Monitor, config );
                Throw.DebugAssert( fC != null );
                var f = fC.CreateAsyncPredicate();
                Throw.DebugAssert( f != null );
                (await f( "This one will always be the Sync one" )).Should().BeTrue();
                (await f( "NOT" + "HERE!" )).Should().BeFalse();
            }

        }

    }
}
