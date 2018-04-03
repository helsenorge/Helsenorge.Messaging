using System;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    /// <summary>
    /// Handles received messages from the synchronous queue
    /// </summary>
    public class SynchronousMessageListener : MessageListener
    {
        /// <summary>
        /// Specifies the name of the queue this listener is reading from
        /// </summary>
        protected override string QueueName => ResolveQueueName((x) => x.SynchronousQueueName);
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected override QueueType QueueType => QueueType.Synchronous;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="core">Reference to core service bus infrastructure</param>
        /// <param name="logger">Logger used for diagnostic purposes</param>
        /// <param name="messagingNotification">A reference to the messaging notification system</param>
        internal SynchronousMessageListener(
            ServiceBusCore core,
            ILogger logger, 
            IMessagingNotification messagingNotification) : base(core, logger, messagingNotification)
        {
            ReadTimeout = Core.Settings.Synchronous.ReadTimeout;
        }
        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected override void NotifyMessageProcessingStarted(IncomingMessage message)
        {
            MessagingNotification.NotifySynchronousMessageReceivedStarting(message);
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override void NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message)
        {
            var reply = MessagingNotification.NotifySynchronousMessageReceived(message);
            if (reply == null)
            {
                throw new InvalidOperationException($"Message handler for function {message.MessageFunction} returned null");
            }

            var outgoingMessage = new OutgoingMessage()
            {
                ToHerId = message.FromHerId,
                Payload =  reply,
                MessageFunction = message.MessageFunction,
                MessageId = Guid.NewGuid().ToString()
            };
            Task.WaitAll(Core.Send(Logger, outgoingMessage, QueueType.SynchronousReply, rawMessage.ReplyTo, rawMessage.CorrelationId));
        }
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected override void NotifyMessageProcessingCompleted(IncomingMessage message)
        {
            MessagingNotification.NotifySynchronousMessageReceivedCompleted(message);
        }
    }
}
