using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Poco.Mixer
{
    public sealed class MixerResult<T> where T : class, IPoco
    {
        readonly MixerProcessor.UntypedMixerResult _r;

        internal MixerResult( MixerProcessor.UntypedMixerResult r ) => _r = r;

        public bool Success => _r._success;

        public IReadOnlyList<T> Outputs => Unsafe.As<IReadOnlyList<T>>( _r._outputs );

        public IReadOnlyList<UserMessage> Messages => _r._messages;

        public IReadOnlyList<IPoco> Rejected => _r._rejected;
    }
}
