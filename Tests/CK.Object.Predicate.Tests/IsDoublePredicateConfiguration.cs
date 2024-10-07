using CK.Core;

namespace CK.Object.Predicate;

public sealed class IsDoublePredicateConfiguration : IsTypePredicateConfiguration<double>
{
    public IsDoublePredicateConfiguration( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )
        : base( monitor, builder, configuration )
    {
    }
}
