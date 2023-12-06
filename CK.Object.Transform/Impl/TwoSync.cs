using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CK.Object.Transform
{
    sealed class TwoSync : ObjectTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly ImmutableArray<ObjectTransformConfiguration> _t;

        public TwoSync( string configurationPath, ImmutableArray<ObjectTransformConfiguration> two )
            : base( configurationPath ) 
        {
            Throw.DebugAssert( two.Length == 2 );
            _t = two;
        }

        public TwoSync( string configurationPath, ObjectTransformConfiguration first, ObjectTransformConfiguration second )
            : this( configurationPath, ImmutableArray.Create( first, second ) ) 
        {
        }

        public IReadOnlyList<ObjectAsyncTransformConfiguration> Transforms => _t;

        public override Func<object, object>? CreateTransform( IServiceProvider services )
        {
            var f = _t[0].CreateTransform( services );
            var s = _t[1].CreateTransform( services );
            if( f != null )
            {
                if( s != null )
                {
                    return o => s( f( o ) );
                }
                return f;
            }
            return s;
        }

        public override ObjectTransformDescriptor? CreateDescriptor( TransformDescriptorContext context, IServiceProvider services )
        {
            return TwoAsync.CreateDescriptor( this, context, services, ImmutableArray<ObjectAsyncTransformConfiguration>.CastUp( _t ) );
        }
    }

}
