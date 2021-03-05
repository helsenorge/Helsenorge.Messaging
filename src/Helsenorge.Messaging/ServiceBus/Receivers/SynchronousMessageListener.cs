/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

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
        protected override async Task NotifyMessageProcessingStarted(IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedStarting), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifySynchronousMessageReceivedStarting(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedStarting), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override async Task NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceived), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            var reply = await MessagingNotification.NotifySynchronousMessageReceived(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceived), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);

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
        protected override async Task NotifyMessageProcessingCompleted(IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedCompleted), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifySynchronousMessageReceivedCompleted(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedCompleted), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
    }
}
