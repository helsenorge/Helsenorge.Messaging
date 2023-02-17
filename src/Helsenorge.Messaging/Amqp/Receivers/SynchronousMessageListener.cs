/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Threading.Tasks;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Amqp.Receivers
{
    /// <summary>
    /// Handles received messages from the synchronous queue
    /// </summary>
    public class SynchronousMessageListener : MessageListener
    {
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected override QueueType QueueType => QueueType.Synchronous;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronousMessageListener"/> class.
        /// </summary>
        /// <param name="amqpCore">An instance of <see cref="AmqpCore"/> which has the common infrastructure to talk to the Message Bus.</param>
        /// <param name="logger">An instance of <see cref="ILogger"/>, used to log diagnostics information.</param>
        /// <param name="messagingNotification">An instance of <see cref="IMessagingNotification"/> which holds reference to callbacks back to the client that owns this instance of the <see cref="MessageListener"/>.</param>
        /// <param name="queueNames">The Queue Names associated with the client.</param>
        internal SynchronousMessageListener(
            AmqpCore amqpCore,
            ILogger logger, 
            IMessagingNotification messagingNotification,
            QueueNames queueNames) : base(amqpCore, logger, messagingNotification, queueNames)
        {
            ReadTimeout = AmqpCore.Settings.Synchronous.ReadTimeout;
        }
        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected override async Task NotifyMessageProcessingStartedAsync(MessageListener listener, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedStartingAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifySynchronousMessageReceivedStartingAsync(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedStartingAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override async Task NotifyMessageProcessingReadyAsync(IAmqpMessage rawMessage, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            var reply = await MessagingNotification.NotifySynchronousMessageReceivedAsync(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);

            if (reply == null)
            {
                throw new InvalidOperationException($"Message handler for function {message.MessageFunction} returned null");
            }

            var outgoingMessage = new OutgoingMessage()
            {
                FromHerId = message.ToHerId,
                ToHerId = message.FromHerId,
                Payload =  reply,
                MessageFunction = message.MessageFunction,
                MessageId = Guid.NewGuid().ToString()
            };
            await AmqpCore.SendAsync(Logger, outgoingMessage, QueueType.SynchronousReply, rawMessage.ReplyTo, rawMessage.CorrelationId).ConfigureAwait(false);
        }
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected override async Task NotifyMessageProcessingCompletedAsync(IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedCompletedAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifySynchronousMessageReceivedCompletedAsync(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceivedCompletedAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
    }
}
