using CK.Core;
using System;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Hook implementation for synchronous transform functions.
    /// </summary>
    public partial class ObjectTransformHook : IObjectTransformHook
    {
        readonly TransformHookContext _context;
        readonly IObjectTransformConfiguration _configuration;
        readonly Func<object, object> _transform;

        /// <summary>
        /// Initializes a new hook.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">The transform configuration.</param>
        /// <param name="transform">The transform function.</param>
        public ObjectTransformHook( TransformHookContext context, IObjectTransformConfiguration configuration, Func<object, object> transform )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( transform );
            _context = context;
            _configuration = configuration;
            _transform = transform;
        }

        /// <summary>
        /// Constructor used by <see cref="SequenceTransformHook"/>. Must be used by specialized hook when the transform
        /// configuration contains other <see cref="ObjectTransformConfiguration"/> to expose the internal function structure.
        /// </summary>
        /// <param name="context">The hook context.</param>
        /// <param name="configuration">This configuration.</param>
        protected ObjectTransformHook( TransformHookContext context, IObjectTransformConfiguration configuration )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            _context = context;
            _configuration = configuration;
            _transform = null!;
        }

        /// <inheritdoc />
        public IObjectTransformConfiguration Configuration => _configuration;

        /// <inheritdoc />
        public TransformHookContext Context => _context;

        /// <inheritdoc />
        public ObjectTransformHook? Synchronous => this;

        ValueTask<object> IObjectTransformHook.TransformAsync( object o ) => ValueTask.FromResult( Transform( o ) );

        /// <summary>
        /// Applies the transformation.
        /// </summary>
        /// <param name="o">The object to transform.</param>
        /// <returns>The transformed object.</returns>
        public object Transform( object o )
        {
            object? r = _context.OnBeforeTransform( this, o );
            if( r != null ) return r;
            try
            {
                r = DoTransform( o );
                if( r == null )
                {
                    Throw.InvalidOperationException( $"Transform '{_configuration.ConfigurationPath}' returned a null reference." );
                }
                return _context.OnAfterTransform( this, o, r ) ?? r;
            }
            catch( Exception ex )
            {
                r = _context.OnTransformError( this, o, ex );
                if( r == null )
                {
                    throw;
                }
                return r;
            }
        }

        /// <summary>
        /// Actual application of the transform function.
        /// </summary>
        /// <param name="o">The object to transform.</param>
        /// <returns>The transformation result.</returns>
        protected virtual object DoTransform( object o ) => _transform( o );
    }

}
