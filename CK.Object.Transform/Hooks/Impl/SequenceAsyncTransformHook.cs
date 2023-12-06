using CK.Core;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook implementation for sequence of asynchronous transform functions.
    /// </summary>
    public class SequenceAsyncTransformHook : ObjectAsyncTransformHook, ISequenceTransformHook
    {
        readonly ImmutableArray<IObjectTransformHook> _transforms;

        public SequenceAsyncTransformHook( TransformHookContext context, ISequenceTransformConfiguration configuration, ImmutableArray<IObjectTransformHook> transforms )
            : base( context, configuration )
        {
            Throw.CheckNotNullArgument( transforms );
            _transforms = transforms;
        }
        public new ISequenceTransformConfiguration Configuration => Unsafe.As<ISequenceTransformConfiguration>( base.Configuration );

        public ImmutableArray<IObjectTransformHook> Transforms => _transforms;

        protected override async ValueTask<object> DoTransformAsync( object o )
        {
            // Breaks on a null result: base.TransformAsync will throw the InvalidOperationException. 
            foreach( var i in _transforms )
            {
                o = await i.TransformAsync( o ).ConfigureAwait( false );
                if( o == null ) break;
            }
            return o!;
        }
    }

}
