using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    public sealed class IsInTextFileAsyncPredicateConfiguration : ObjectAsyncPredicateConfiguration
    {
        readonly string _fileName;

        public IsInTextFileAsyncPredicateConfiguration( IActivityMonitor monitor,
                                                        TypedConfigurationBuilder builder,
                                                        ImmutableConfigurationSection configuration )
            : base( configuration.Path )
        {
            _fileName = IsInTextFilePredicateConfiguration.ReadFileName( monitor, configuration );
        }

        public override Func<object, ValueTask<bool>> CreateAsyncPredicate( IServiceProvider services )
        {
            return async o =>
            {
                var needle = o.ToString() ?? "";
                if( needle.Length == 0 ) return false;
                var t = await System.IO.File.ReadAllTextAsync( _fileName );
                return t.Contains( needle );
            };
        }
    }

}
