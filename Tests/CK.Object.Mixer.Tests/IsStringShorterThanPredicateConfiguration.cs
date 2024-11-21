using CK.Core;
using System;

namespace CK.Object.Predicate;

public sealed class IsStringShorterThanPredicateConfiguration : ObjectPredicateConfiguration
{
    readonly int _len;

    public IsStringShorterThanPredicateConfiguration( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )
        : base( configuration.Path )
    {
        _len = ReadLength( monitor, configuration );
    }

    internal static int ReadLength( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
    {
        int result;
        var c = configuration.TryGetIntValue( monitor, "Length", 1 );
        if( c == null )
        {
            monitor.Error( $"Missing or invalid '{configuration.Path}:Length' value." );
            result = 0;
        }
        else
        {
            result = c.Value;
        }
        return result;
    }


    public override Func<object, bool> CreatePredicate( IServiceProvider services )
    {
        return o => o is string s && s.Length < _len;
    }
}
