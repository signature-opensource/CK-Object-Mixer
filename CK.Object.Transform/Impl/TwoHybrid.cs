using CK.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    sealed class TwoHybrid : ObjectAsyncTransformConfiguration, ISequenceTransformConfiguration
    {
        readonly ObjectAsyncTransformConfiguration[] _t;
        readonly bool _revert;

        public TwoHybrid( string configurationPath, ObjectTransformConfiguration first, ObjectAsyncTransformConfiguration second, bool revert )
            : base( configurationPath )
        {
            _t = new[] { first, second };
            _revert = revert;
        }

        public IReadOnlyList<IObjectTransformConfiguration> Transforms => _t;

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

        public override IObjectTransformHook? CreateAsyncHook( TransformHookContext context, IServiceProvider services )
        {
            var f = Unsafe.As<ObjectTransformConfiguration>( _t[0] ).CreateHook( context, services );
            var s = _t[1].CreateAsyncHook( context, services );
            if( f != null )
            {
                if( s != null )
                {
                    return new TwoHookHybrid( context, this, f, s, _revert );
                }
                return f;
            }
            return s;
        }


    }

}
