using CK.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CK.Object.Predicate;

/// <summary>
/// Not operator.
/// </summary>
public sealed class NotPredicateConfiguration : ObjectPredicateConfiguration
{
    [AllowNull]
    readonly ObjectPredicateConfiguration _operand;

    /// <summary>
    /// Required constructor.
    /// </summary>
    /// <param name="monitor">Unused monitor.</param>
    /// <param name="builder">Unused builder.</param>
    /// <param name="configuration">Captured configuration.</param>
    public NotPredicateConfiguration( IActivityMonitor monitor,
                                      TypedConfigurationBuilder builder,
                                      ImmutableConfigurationSection configuration )
        : base( configuration.Path )
    {
        var cOperand = configuration.TryGetSection( "Operand" );
        if( cOperand == null )
        {
            monitor.Error( $"Missing '{configuration.Path}:Operand' configuration." );
        }
        else
        {
            _operand = builder.Create<ObjectPredicateConfiguration>( monitor, cOperand );
        }
    }

    /// <summary>
    /// Gets the operand that is negated.
    /// </summary>
    public ObjectPredicateConfiguration Operand => _operand;

    /// <inheritdoc />
    public override Func<object, bool>? CreatePredicate( IServiceProvider services )
    {
        var p = _operand.CreatePredicate( services );
        return p != null ? o => !p( o ) : null;
    }
}
