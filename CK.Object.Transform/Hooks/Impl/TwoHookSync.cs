namespace CK.Object.Transform
{
    sealed class TwoHookSync : ObjectTransformHook
    {
        readonly ObjectTransformHook _first;
        readonly ObjectTransformHook _second;

        public TwoHookSync( TransformHookContext context,
                     IObjectTransformConfiguration configuration,
                     ObjectTransformHook first,
                     ObjectTransformHook second )
            : base( context, configuration )
        {
            _first = first;
            _second = second;
        }

        protected override object DoTransform( object o )
        {
            return _second.Transform( _first.Transform( o ) );
        }
    }

}
