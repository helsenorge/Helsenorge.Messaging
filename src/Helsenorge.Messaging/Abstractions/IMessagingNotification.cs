/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.ServiceBus.Receivers;
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
        /// Called to notify that an asynchronous message is ready for processing
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifyAsynchronousMessageReceived(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process an asynchronous message
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Information about the message</param>
        Task NotifyAsynchronousMessageReceivedStarting(MessageListener listener, IncomingMessage message);
        /// <summary>
        /// Called to notify that we are finished processing an asynchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifyAsynchronousMessageReceivedCompleted(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are ready to process an error message
        /// </summary>
        /// <param name="message"></param>
        Task NotifyErrorMessageReceived(IMessagingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process an error message
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifyErrorMessageReceivedStarting(IncomingMessage message);
        /// <summary>
        /// Called to notify that a synchronous message is ready for processing
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task<XDocument> NotifySynchronousMessageReceived(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are finished to processing a synchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifySynchronousMessageReceivedCompleted(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process a synchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifySynchronousMessageReceivedStarting(IncomingMessage message);

        /// <summary>
        /// Called to notifiy that we have an unhandled exception
        /// </summary>
        /// <param name="message">Information about the incoming message</param>
        /// <param name="ex">The exception</param>
        Task NotifyUnhandledException(IMessagingMessage message, Exception ex);

        /// <summary>
        /// Called to notifiy that we have a handled exception
        /// </summary>
        /// <param name="message">Information about the incoming message</param>
        /// <param name="ex">The exception</param>
        Task NotifyHandledException(IMessagingMessage message, Exception ex);
    }
}
