using CK.Core;
using System;

namespace CK.Object.Predicate;

public sealed class IsStringShorterThanPredicateConfiguration : ObjectPredicateConfiguration
{
    readonly int _len;

    public IsStringShorterThanPredicateConfiguration( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )
        : base( configuration.Path )
    {
        _len = IsStringLongerThanPredicateConfiguration.ReadLength( monitor, configuration );
    }

    public override Func<object, bool> CreatePredicate( IServiceProvider services )
    {
        return o => o is string s && s.Length < _len;
    }
}
