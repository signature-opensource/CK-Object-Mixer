using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CK.Object.Transform
{
    /// <summary>
    /// Descriptor for transformations.
    /// </summary>
    public sealed class ObjectTransformDescriptor
    {
        readonly TransformDescriptorContext _context;
        readonly IObjectTransformConfiguration _configuration;
        readonly ImmutableArray<ObjectTransformDescriptor> _transforms;
        readonly Delegate? _transform;
        readonly bool _isSync;

        /// <summary>
        /// Initializes a new <see cref="ObjectTransformDescriptor"/> for a simple transform configuration.
        /// </summary>
        /// <param name="context">The descriptor context.</param>
        /// <param name="configuration">The transform configuration.</param>
        /// <param name="transform">The transform function.</param>
        public ObjectTransformDescriptor( TransformDescriptorContext context, IObjectTransformConfiguration configuration, Delegate transform )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckNotNullArgument( transform );
            _isSync = transform is Func<object, object>;
            if( !_isSync && transform is not Func<object, ValueTask<object>> )
            {
                Throw.ArgumentException( nameof( transform ), "Must be Func<object, object> or Func<object,ValueTask<object>>." );
            }
            _context = context;
            _configuration = configuration;
            _transform = transform;
        }

        internal ObjectTransformDescriptor( TransformDescriptorContext context,
                                            ISequenceTransformConfiguration configuration,
                                            ImmutableArray<ObjectTransformDescriptor> transforms )
        {
            Throw.CheckNotNullArgument( context );
            Throw.CheckNotNullArgument( configuration );
            Throw.CheckArgument( transforms.Length > 1 );
            _isSync = transforms.All( p => p.IsSynchronous );
            _context = context;
            _configuration = configuration;
            _transforms = transforms;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IObjectTransformConfiguration Configuration => _configuration;

        /// <summary>
        /// Gets whether this transformation is synchronous or asynchronous.
        /// When true <see cref="TransformSync(object)"/> can be called instead of <see cref="TransformAsync(object)"/>
        /// </summary>
        public bool IsSynchronous => _isSync;

        /// <summary>
        /// Gets whether this is a group or a simple predicate.
        /// </summary>
        [MemberNotNullWhen( true, nameof( SequenceConfiguration ), nameof( Descriptors ) )]
        public bool IsSequence => !_transforms.IsDefault;

        /// <summary>
        /// Gets the description of the group if <see cref="IsGroup"/> is true otherwise null.
        /// </summary>
        public ISequenceTransformConfiguration? SequenceConfiguration => _configuration as ISequenceTransformConfiguration;

        /// <summary>
        /// Gets the descriptors of the sequence if <see cref="IsSequence"/> is true otherwise null.
        /// </summary>
        public IReadOnlyList<ObjectTransformDescriptor>? Descriptors => _transforms.IsDefault ? null : _transforms;

        /// <summary>
        /// Gets the descriptor context to which this descriptor is bound.
        /// </summary>
        public TransformDescriptorContext Context => _context;

        /// <summary>
        /// Applies the transformation asynchronously.
        /// This is always available.
        /// </summary>
        /// <param name="o">The object to transform.</param>
        /// <returns>The transformed object.</returns>
        public async ValueTask<object> TransformAsync( object o )
        {
            if( _isSync )
            {
                return TransformSync( o );
            }
            object? r = _context.OnBeforeTransform( this, o );
            if( r != null ) return r;
            try
            {
                if( _transform != null )
                {
                    r = await Unsafe.As<Func<object, ValueTask<object>>>( _transform )( o );
                }
                else
                {
                    r = await SequenceTransformAsync( o, _transforms );
                }
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

            static async ValueTask<object> SequenceTransformAsync( object o, ImmutableArray<ObjectTransformDescriptor> transforms )
            {
                // Breaks on a null result: TransformAsync will throw the InvalidOperationException. 
                foreach( var i in transforms )
                {
                    o = await i.TransformAsync( o ).ConfigureAwait( false );
                    if( o == null ) break;
                }
                return o!;
            }

        }

        /// <summary>
        /// Synchronously applies the transformation. Must be called only if <see cref="IsSynchronous"/> is true
        /// otherwise an <see cref="InvalidOperationException"/> is thrown.
        /// </summary>
        /// <param name="o">The object to transform.</param>
        /// <returns>The transformed object.</returns>
        public object TransformSync( object o )
        {
            Throw.CheckState( IsSynchronous );
            object? r = _context.OnBeforeTransform( this, o );
            if( r != null ) return r;
            try
            {
                if( _transform != null )
                {
                    r = Unsafe.As<Func<object, object>>( _transform )( o );
                }
                else
                {
                    r = SequenceTransform( o, _transforms );
                }
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

            static object SequenceTransform( object o, ImmutableArray<ObjectTransformDescriptor> transforms )
            {
                // Breaks on a null result: TransformSync will throw the InvalidOperationException. 
                foreach( var i in transforms )
                {
                    o = i.TransformSync( o );
                    if( o == null ) break;
                }
                return o!;
            }
        }

    }

}
