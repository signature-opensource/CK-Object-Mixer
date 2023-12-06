using System.Threading.Tasks;

namespace CK.Object.Transform
{
    sealed class TwoHookAsync : ObjectAsyncTransformHook
    {
        readonly IObjectTransformHook _first;
        readonly IObjectTransformHook _second;

        public TwoHookAsync( TransformHookContext context,
                             IObjectTransformConfiguration configuration,
                             IObjectTransformHook first,
                             IObjectTransformHook second )
            : base( context, configuration )
        {
            _first = first;
            _second = second;
        }

        protected override async ValueTask<object> DoTransformAsync( object o )
        {
            return await _second.TransformAsync( await _first.TransformAsync( o ).ConfigureAwait( false ) ).ConfigureAwait( false );
        }
    }

    sealed class TwoHookHybrid : ObjectAsyncTransformHook
    {
        readonly ObjectTransformHook _first;
        readonly IObjectTransformHook _second;
        readonly bool _revert;

        public TwoHookHybrid( TransformHookContext context,
                              IObjectTransformConfiguration configuration,
                              ObjectTransformHook first,
                              IObjectTransformHook second,
                              bool revert )
            : base( context, configuration )
        {
            _first = first;
            _second = second;
            _revert = revert;
        }

        protected override async ValueTask<object> DoTransformAsync( object o )
        {
            return _revert
                    ? _first.Transform( await _second.TransformAsync( o ).ConfigureAwait( false ) )
                    : await _second.TransformAsync( _first.Transform( o ) ).ConfigureAwait( false );
        }
    }

}
