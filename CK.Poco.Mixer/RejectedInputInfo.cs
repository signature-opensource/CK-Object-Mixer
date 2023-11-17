using CK.Core;
using System.Collections.Immutable;

namespace CK.Poco.Mixer
{
    /// <summary>
    /// Captures a rejected input along with messages explaining why it has been rejected.
    /// </summary>
    /// <param name="Input">The rejected input.</param>
    /// <param name="Messages">Messages (errors, warnings or informations) that explain the rejection.</param>
    public record struct RejectedInputInfo( IPoco Input, ImmutableArray<UserMessage> Messages );

}
