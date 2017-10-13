using System;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    /// <summary>
    /// Listens for the reply to an outgoing synchronous message
    /// </summary>
    public class SynchronousReplyListener : MessageListener
    {
        /// <summary>
        /// Specifies the name of the queue this listener is reading from
        /// </summary>
        protected override string QueueName => Core.Settings.Synchronous.FindReplyQueueForMe();
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected override QueueType QueueType => QueueType.Synchronous;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="core">Reference to core service bus infrastructure</param>
        /// <param name="logger">Logger used for diagnostic purposes</param>
        /// <param name="messagingNotification"></param>
        internal SynchronousReplyListener(ServiceBusCore core, ILogger logger, IMessagingNotification messagingNotification) : base(core, logger, messagingNotification)
        {
            ReadTimeout = Core.Settings.Synchronous.ReadTimeout;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected override void NotifyMessageProcessingStarted(IncomingMessage message)
        {
            Logger.LogDebug("NotifyMessageProcessingStarted");
            // Not relevant for this implementation
        }
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected override void NotifyMessageProcessingCompleted(IncomingMessage message)
        {
            Logger.LogDebug("NotifyMessageProcessingCompleted");
            // Not relevant for this implementation
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override void NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message)
        {
            Logger.LogDebug("NotifyMessageProcessingReady");
            MessagingNotification.NotifySynchronousMessageReceived(message);
        }
    }
}
