using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        void NotifyAsynchronousMessageReceived(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process an asynchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        void NotifyAsynchronousMessageReceivedStarting(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are finished processing an asynchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        void NotifyAsynchronousMessageReceivedCompleted(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are ready to process an error message
        /// </summary>
        /// <param name="message"></param>
        void NotifyErrorMessageReceived(IMessagingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process an error message
        /// </summary>
        /// <param name="message">Information about the message</param>
        void NotifyErrorMessageReceivedStarting(IncomingMessage message);
        /// <summary>
        /// Called to notify that a synchronous message is ready for processing
        /// </summary>
        /// <param name="message">Information about the message</param>
        XDocument NotifySynchronousMessageReceived(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are finished to processing a synchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        void NotifySynchronousMessageReceivedCompleted(IncomingMessage message);
        /// <summary>
        /// Called to notify that we are starting to process a synchronous message
        /// </summary>
        /// <param name="message">Information about the message</param>
        void NotifySynchronousMessageReceivedStarting(IncomingMessage message);

        /// <summary>
        /// Called to notifiy that we have a handled exception
        /// </summary>
        /// <param name="message">Information about the incoming message</param>
        /// <param name="ex">The exception</param>
        void NotifyUnhandledException(IMessagingMessage message, Exception ex);

        /// <summary>
        /// Called to notifiy that we have an unhandled exception
        /// </summary>
        /// <param name="message">Information about the incoming message</param>
        /// <param name="ex">The exception</param>
        void NotifyHandledException(IMessagingMessage message, Exception ex);
    }
}
