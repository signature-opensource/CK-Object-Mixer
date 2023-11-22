using CK.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Object.Mixer
{
    public sealed class ObjectMixerResult<T> where T : class
    {
        readonly ObjectMixerProcessor.UntypedMixerResult _r;

        public ObjectMixerResult( ObjectMixerProcessor.UntypedMixerResult r ) => _r = r;

        public ObjectMixerResult( UserMessageCollector? userMessages ) => _r = new ObjectMixerProcessor.UntypedMixerResult( false, null, null, userMessages );

        public bool Success => _r._success;

        public IReadOnlyList<T> Outputs => Unsafe.As<IReadOnlyList<T>>( _r._outputs );

        public IReadOnlyList<UserMessage> Messages => _r._userMessages;

        public IReadOnlyList<object> Rejected => _r._rejected;
    }
}
