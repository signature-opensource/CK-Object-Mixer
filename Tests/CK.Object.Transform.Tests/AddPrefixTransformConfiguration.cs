using CK.Core;
using System;

namespace CK.Object.Transform;

public sealed class AddPrefixTransformConfiguration : ObjectTransformConfiguration
{
    readonly string _prefix;

    public AddPrefixTransformConfiguration( IActivityMonitor monitor,
                                            TypedConfigurationBuilder builder,
                                            ImmutableConfigurationSection configuration )
        : base( configuration.Path )
    {
        _prefix = configuration["Prefix"] ?? "";
    }

    public override Func<object, object>? CreateTransform( IServiceProvider services )
    {
        return Transform;
    }

    object Transform( object o )
    {
        if( o is not string s )
        {
            throw new ArgumentException( $"String expected, got '{o.GetType().ToCSharpName()}'." );
        }
        return _prefix + s;
    }
}
