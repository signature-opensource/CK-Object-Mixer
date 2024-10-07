using CK.Core;
using System;

namespace CK.Object.Predicate;

public sealed class IsGreaterThanPredicateConfiguration : ObjectPredicateConfiguration
{
    readonly string _value;

    public IsGreaterThanPredicateConfiguration( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )
        : base( configuration.Path )
    {
        _value = configuration["Value"] ?? "";
    }

    public override Func<object, bool>? CreatePredicate( IServiceProvider services )
    {
        return o => Impl( o, _value );
    }

    static bool Impl( object o, string value )
    {
        return o switch
        {
            double d => d > double.Parse( value ),
            int i => i > int.Parse( value ),
            string s => s.CompareTo( value ) > 0,
            _ => Throw.ArgumentException<bool>( nameof( value ) )
        };
    }
}
