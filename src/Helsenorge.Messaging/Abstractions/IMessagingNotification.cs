/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Amqp.Receivers;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Defines an interface where we can notify different message processing stages
    /// </summary>
    public interface IMessagingNotification
    {
        /// <summary>
        /// Called when the asynchronous message is ready for processing.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifyAsynchronousMessageReceivedAsync(IncomingMessage message);

        /// <summary>
        /// Called to notify that processing of an asynchronous message has started. The client can do any necessary
        /// set up it needs, like setting up CorrelationIds, etc.
        /// </summary>
        /// <param name="listener">Reference to the listener invoking the callback.</param>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifyAsynchronousMessageReceivedStartingAsync(MessageListener listener, IncomingMessage message);

        /// <summary>
        /// Called when the asynchronous message has been successfully processed.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifyAsynchronousMessageReceivedCompletedAsync(IncomingMessage message);

        /// <summary>
        /// Called when the error message is ready for processing.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifyErrorMessageReceivedAsync(IAmqpMessage message);

        /// <summary>
        /// Called to notify that processing of an error message has started. The client can do any necessary set up it
        /// needs, like setting up CorrelationIds, etc.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifyErrorMessageReceivedStartingAsync(IncomingMessage message);

        /// <summary>
        /// Called when the synchronous message is ready for processing. The client should process the synchronous
        /// message here and return the XDocument that is the output to the caller.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        /// <returns>Returns the XDocument that is to be delivered to the synchronous caller.</returns>
        Task<XDocument> NotifySynchronousMessageReceivedAsync(IncomingMessage message);

        /// <summary>
        /// Called when the synchronous message has been successfully processed.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifySynchronousMessageReceivedCompletedAsync(IncomingMessage message);

        /// <summary>
        /// Called to notify that we haved started to process a synchronous message.
        /// The client can do any necessary set up it needs, like setting up CorrelationIds, etc.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifySynchronousMessageReceivedStartingAsync(IncomingMessage message);

        /// <summary>
        /// Called when the synchronous message is ready for processing. The client should process the synchronous
        /// message here and return the XDocument that is the output to the caller.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        /// <returns>Returns the XDocument that is to be delivered to the synchronous caller.</returns>
        Task<XDocument> NotifySynchronousReplyMessageReceivedAsync(IncomingMessage message);

        /// <summary>
        /// Called when the synchronous message has been successfully processed.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifySynchronousReplyMessageReceivedCompletedAsync(IncomingMessage message);

        /// <summary>
        /// Called to notify that processing of an syncreply message has started. The client can do any necessary
        /// set up it needs, like setting up CorrelationIds, etc.
        /// </summary>
        /// <param name="listener">Reference to the listener invoking the callback.</param>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        Task NotifySynchronousReplyMessageReceivedStartingAsync(MessageListener listener, IncomingMessage message);

        /// <summary>
        /// Called to notifiy an unhandled exception has occurred. The client need to assert what it needs to do.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        /// <param name="ex">The exception that occurred.</param>
        Task NotifyUnhandledExceptionAsync(IAmqpMessage message, Exception ex);

        /// <summary>
        /// Called to notify that an exception has occurred and that it has been handled.
        /// </summary>
        /// <param name="message">The actual message, contains the payload in addition to metadata.</param>
        /// <param name="ex">The exception that occurred.</param>
        Task NotifyHandledExceptionAsync(IAmqpMessage message, Exception ex);
    }
}
