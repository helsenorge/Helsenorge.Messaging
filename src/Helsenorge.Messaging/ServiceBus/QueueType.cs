namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// Specifies type of queue
    /// </summary>
    public enum QueueType
    {
        /// <summary>
        /// Queue used for asynchronous messages
        /// </summary>
        Asynchronous,
        /// <summary>
        /// Queue used for synchronous messages
        /// </summary>
        Synchronous,
        /// <summary>
        /// Queue used for error messages
        /// </summary>
        Error,
        /// <summary>
        /// Queue used for synchronous reply messages
        /// </summary>
        SynchronousReply
    }
}
