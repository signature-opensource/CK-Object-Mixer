using CK.Core;
using System;

namespace CK.Object.Predicate
{
    public sealed class IsInTextFilePredicateConfiguration : ObjectPredicateConfiguration
    {
        readonly string _fileName;

        public IsInTextFilePredicateConfiguration( IActivityMonitor monitor,
                                                   TypedConfigurationBuilder builder,
                                                   ImmutableConfigurationSection configuration )
            : base( configuration.Path )
        {
            _fileName = ReadFileName( monitor, configuration );
        }

        internal static string ReadFileName( IActivityMonitor monitor, ImmutableConfigurationSection configuration )
        {
            var f = configuration["FileName"];
            if( string.IsNullOrWhiteSpace( f ) )
            {
                monitor.Error( $"Missing '{configuration.Path}:FileName' value." );
            }
            return f!;
        }

        public override Func<object, bool> CreatePredicate( IServiceProvider services )
        {
            return o =>
            {
                var needle = o.ToString() ?? "";
                return needle.Length != 0 && System.IO.File.ReadAllText( _fileName ).Contains( needle );
            };
        }
    }

}
