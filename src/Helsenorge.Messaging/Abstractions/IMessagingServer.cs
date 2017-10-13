using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Main interface for receiving messages
    /// </summary>
    public interface IMessagingServer
    {
        /// <summary>
        /// Start message processing
        /// </summary>
        void Start();
        /// <summary>
        /// Terminate message processing
        /// </summary>
        /// <param name="timeout">The amount of time we wait for things to shut down</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop")]
        void Stop(TimeSpan timeout);
        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedStartingCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedCompletedCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedStartingCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedCallback(Func<IncomingMessage, XDocument> action);
        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedCompletedCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we receive an error message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterErrorMessageReceivedCallback(Action<IMessagingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we have an handled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterHandledExceptionCallback(Action<IMessagingMessage, Exception> action);
        /// <summary>
        /// Registers a delegate that should be called when we have an unhandled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterUnhandledExceptionCallback(Action<IMessagingMessage, Exception> action);
    }
}
