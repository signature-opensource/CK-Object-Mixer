using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    sealed class TwoHybrid : ObjectAsyncTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly ImmutableArray<ObjectAsyncTransformConfiguration> _t;
        readonly bool _revert;

        public TwoHybrid( string configurationPath, ObjectTransformConfiguration first, ObjectAsyncTransformConfiguration second, bool revert )
            : base( configurationPath )
        {
            _t = ImmutableArray.Create( first, second );
            _revert = revert;
        }

        public IReadOnlyList<ObjectAsyncTransformConfiguration> Transforms => _t;

        public override Func<object, ValueTask<object>>? CreateAsyncTransform( IServiceProvider services )
        {
            var f = Unsafe.As<ObjectTransformConfiguration>( _t[0] ).CreateTransform( services );
            var s = _t[1].CreateAsyncTransform( services );
            if( f != null )
            {
                if( s != null )
                {
                    return _revert
                             ? async o => f( await s( o ).ConfigureAwait( false ) ) 
                             : async o => await s( f( o ) ).ConfigureAwait( false );
                }
                return o => ValueTask.FromResult( f( o ) );
            }
            return s;
        }

        public override ObjectTransformDescriptor? CreateDescriptor( TransformDescriptorContext context, IServiceProvider services )
        {
            var l = _t[0].CreateDescriptor( context, services );
            var r = _t[1].CreateDescriptor( context, services );
            if( l != null )
            {
                if( r != null )
                {
                    var p = _revert ? ImmutableArray.Create( r, l ) : ImmutableArray.Create( l, r );
                    return new ObjectTransformDescriptor( context, this, p );
                }
                return l;
            }
            return r;
        }


    }

}
