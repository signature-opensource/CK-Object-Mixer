using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Object.Predicate
{
    public sealed class EnumerableMaxCountPredicateConfiguration : ObjectPredicateConfiguration
    {
        readonly int _maxCount;

        public EnumerableMaxCountPredicateConfiguration( IActivityMonitor monitor, TypedConfigurationBuilder builder, ImmutableConfigurationSection configuration )
            : base( configuration.Path )
        {
            var c = configuration.TryGetIntValue( monitor, "MaxCount" );
            if( !c.HasValue )
            {
                monitor.Error( $"Missing '{configuration.Path}:MaxCount' value." );
            }
            else _maxCount = c.Value;
        }

        public override Func<object, bool> CreatePredicate( IServiceProvider services )
        {
            return o => DoContains( o, _maxCount );
        }

        static bool DoContains( object o, int maxCount )
        {
            if( o is System.Collections.IEnumerable e )
            {
                int c = 0;
                foreach( var item in e )
                {
                    if( ++c == maxCount )
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }

}
