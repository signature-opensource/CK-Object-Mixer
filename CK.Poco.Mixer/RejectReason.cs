namespace CK.Poco.Mixer
{
    /// <summary>
    /// Qualifies the reason why the a mixer rejected an input.
    /// </summary>
    public enum RejectReason
    {
        /// <summary>
        /// Not applicable (the input has been accepted or has not been handled at all).
        /// </summary>
        None,

        /// <summary>
        /// The input has been accepted and must be silently ignored.
        /// </summary>
        IgnoreInput,

        /// <summary>
        /// The input has been accepted but is invalid (kind of 400 Bad Request).
        /// </summary>
        InvalidInput,

        /// <summary>
        /// The input has been accepted but an error prevents its processing (kind of 500 Internal Server Error).
        /// </summary>
        Error
    }

}
