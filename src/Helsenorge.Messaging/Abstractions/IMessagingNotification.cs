/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
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
        /// Called to notify that an asynchronous message is ready for processing
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifyAsynchronousMessageReceivedAsync(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process an asynchronous message
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Information about the message</param>
        Task NotifyAsynchronousMessageReceivedStartingAsync(MessageListener listener, IncomingMessage message);
        /// <summary>
        /// Called to notify that we are finished processing an asynchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifyAsynchronousMessageReceivedCompletedAsync(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are ready to process an error message
        /// </summary>
        /// <param name="message"></param>
        Task NotifyErrorMessageReceivedAsync(IMessagingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process an error message
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifyErrorMessageReceivedStartingAsync(IncomingMessage message);
        /// <summary>
        /// Called to notify that a synchronous message is ready for processing
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task<XDocument> NotifySynchronousMessageReceivedAsync(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are finished to processing a synchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifySynchronousMessageReceivedCompletedAsync(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process a synchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        Task NotifySynchronousMessageReceivedStartingAsync(IncomingMessage message);

        /// <summary>
        /// Called to notifiy that we have an unhandled exception
        /// </summary>
        /// <param name="message">Information about the incoming message</param>
        /// <param name="ex">The exception</param>
        Task NotifyUnhandledExceptionAsync(IMessagingMessage message, Exception ex);

        /// <summary>
        /// Called to notifiy that we have a handled exception
        /// </summary>
        /// <param name="message">Information about the incoming message</param>
        /// <param name="ex">The exception</param>
        Task NotifyHandledExceptionAsync(IMessagingMessage message, Exception ex);
    }
}
