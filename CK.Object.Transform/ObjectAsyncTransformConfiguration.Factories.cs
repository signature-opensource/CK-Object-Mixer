using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CK.Object.Transform;

public abstract partial class ObjectAsyncTransformConfiguration
{
    internal static ObjectAsyncTransformConfiguration DoCreateGroup( string configurationPath,
                                                                     IReadOnlyList<ObjectAsyncTransformConfiguration> predicates )
    {
        if( predicates.All( p => p is ObjectTransformConfiguration ) )
        {
            var syncTransforms = predicates.Cast<ObjectTransformConfiguration>().ToImmutableArray();
            return new SequenceTransformConfiguration( configurationPath, syncTransforms );
        }
        return new SequenceAsyncTransformConfiguration( configurationPath, predicates.ToImmutableArray() );
    }

    /// <summary>
    /// Optimally combines 2 optional transform configurations into a transformation that will execute as synchronously as possible.
    /// </summary>
    /// <param name="configurationPath">A required configuration path for the combination.</param>
    /// <param name="first">Optional first transform configuration.</param>
    /// <param name="second">Optional second transform configuration.</param>
    /// <returns>A "And" group.</returns>
    public static ObjectAsyncTransformConfiguration? Combine( string configurationPath,
                                                              ObjectAsyncTransformConfiguration? first,
                                                              ObjectAsyncTransformConfiguration? second )
    {
        if( first != null )
        {
            if( second != null )
            {
                ObjectTransformConfiguration? sRight;
                var sLeft = first.Synchronous;
                if( sLeft != null )
                {
                    sRight = second.Synchronous;
                    if( sRight != null )
                    {
                        return new TwoSync( configurationPath, sLeft, sRight );
                    }
                    return new TwoHybrid( configurationPath, sLeft, second, false );
                }
                sRight = second.Synchronous;
                if( sRight != null )
                {
                    return new TwoHybrid( configurationPath, sRight, first, true );
                }
                return new TwoAsync( configurationPath, first, second );
            }
            return first;
        }
        return second;
    }
}
