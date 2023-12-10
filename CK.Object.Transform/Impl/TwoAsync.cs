using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    sealed class TwoAsync : ObjectAsyncTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly ImmutableArray<ObjectAsyncTransformConfiguration> _t;

        public TwoAsync( string configurationPath, ObjectAsyncTransformConfiguration first, ObjectAsyncTransformConfiguration second )
            : base( configurationPath )
        {
            _t = ImmutableArray.Create( first, second );
        }

        public IReadOnlyList<ObjectAsyncTransformConfiguration> Transforms => _t;

        public override Func<object, ValueTask<object>>? CreateAsyncTransform( IServiceProvider services )
        {
            var f = _t[0].CreateAsyncTransform( services );
            var s = _t[1].CreateAsyncTransform( services );
            if( f != null )
            {
                if( s != null )
                {
                    return async o => await s( await f( o ).ConfigureAwait( false ) ).ConfigureAwait( false );
                }
                return f;
            }
            return s;
        }

        public override ObjectTransformDescriptor? CreateDescriptor( TransformDescriptorContext context, IServiceProvider services )
        {
            return CreateDescriptor( this, context, services, _t );
        }

        internal static ObjectTransformDescriptor? CreateDescriptor( ISequenceTransformConfiguration c,
                                                                     TransformDescriptorContext context,
                                                                     IServiceProvider services,
                                                                     ImmutableArray<ObjectAsyncTransformConfiguration> transforms )
        {
            var l = transforms[0].CreateDescriptor( context, services );
            var r = transforms[1].CreateDescriptor( context, services );
            if( l != null )
            {
                if( r != null )
                {
                    var p = ImmutableArray.Create( l, r );
                    return new ObjectTransformDescriptor( context, c, p );
                }
                return l;
            }
            return r;
        }
    }

}
