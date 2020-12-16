/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
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
        Task Start();
        /// <summary>
        /// Terminate message processing
        /// </summary>
        /// <param name="timeout">The amount of time we wait for things to shut down</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop")]
        Task Stop(TimeSpan timeout);
        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedStartingCallback(Func<IncomingMessage, Task> action);
        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedCallback(Func<IncomingMessage, Task> action);
        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedCompletedCallback(Func<IncomingMessage, Task> action);
        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedStartingCallback(Func<IncomingMessage, Task> action);
        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedCallback(Func<IncomingMessage, Task<XDocument>> action);
        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedCompletedCallback(Func<IncomingMessage, Task> action);
        /// <summary>
        /// Registers a delegate that should be called when we receive an error message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterErrorMessageReceivedCallback(Func<IMessagingMessage, Task> action);
        /// <summary>
        /// Registers a delegate that should be called when we have an handled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterHandledExceptionCallback(Func<IMessagingMessage, Exception, Task> action);
        /// <summary>
        /// Registers a delegate that should be called when we have an unhandled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterUnhandledExceptionCallback(Func<IMessagingMessage, Exception, Task> action);
    }
}
