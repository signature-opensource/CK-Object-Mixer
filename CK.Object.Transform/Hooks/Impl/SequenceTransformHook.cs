using CK.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook implementation for sequence of synchronous transformations.
    /// </summary>
    public class SequenceTransformHook : ObjectTransformHook, ISequenceTransformHook
    {
        readonly ImmutableArray<ObjectTransformHook> _transforms;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="configuration">The transform configuration.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="transforms">The subordinated transform hook.</param>
        public SequenceTransformHook( TransformHookContext context, ISequenceTransformConfiguration configuration, ImmutableArray<ObjectTransformHook> transforms )
            : base( context, configuration )
        {
            Throw.CheckNotNullArgument( transforms );
            _transforms = transforms;
        }

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="configuration">The transform configuration.</param>
        /// <param name="context">The hook context.</param>
        /// <param name="h">The subordinated transform hook.</param>
        public SequenceTransformHook( TransformHookContext context, ISequenceTransformConfiguration configuration, params ObjectTransformHook[] h )
            : base( context, configuration )
        {
            // Waiting for .NET8: ImmutableCollectionsMarshal.AsImmutableArray(h)
            _transforms = Unsafe.As<ObjectTransformHook[], ImmutableArray<ObjectTransformHook>>( ref h );
        }

        /// <inheritdoc />
        public new ISequenceTransformConfiguration Configuration => Unsafe.As<ISequenceTransformConfiguration>( base.Configuration );

        ImmutableArray<IObjectTransformHook> ISequenceTransformHook.Transforms => ImmutableArray<IObjectTransformHook>.CastUp( _transforms );

        /// <inheritdoc cref="ISequenceTransformHook.Transforms" />
        public ImmutableArray<ObjectTransformHook> Transforms => _transforms;

        /// <inheritdoc />
        protected override object DoTransform( object o )
        {
            // Breaks on a null result: base.Transform will throw the InvalidOperationException. 
            foreach( var i in _transforms )
            {
                o = i.Transform( o );
                if( o == null ) break;
            }
            return o!;
        }
    }

}
