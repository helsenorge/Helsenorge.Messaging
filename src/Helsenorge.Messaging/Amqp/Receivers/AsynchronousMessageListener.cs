/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Amqp.Receivers
{
    /// <summary>
    /// Handles received messages from the asynchronous queue
    /// </summary>
    public class AsynchronousMessageListener : MessageListener
    {
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected override QueueType QueueType => QueueType.Asynchronous;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsynchronousMessageListener"/> class.
        /// </summary>
        /// <param name="amqpCore">An instance of <see cref="AmqpCore"/> which has the common infrastructure to talk to the Message Bus.</param>
        /// <param name="logger">An instance of <see cref="ILogger"/>, used to log diagnostics information.</param>
        /// <param name="messagingNotification">An instance of <see cref="IMessagingNotification"/> which holds reference to callbacks back to the client that owns this instance of the <see cref="MessageListener"/>.</param>
        /// <param name="queueNames">The Queue Names associated with the client.</param>
        internal AsynchronousMessageListener(
            AmqpCore amqpCore,
            ILogger logger,
            IMessagingNotification messagingNotification,
            QueueNames queueNames) : base(amqpCore, logger, messagingNotification, queueNames)
        {
            ReadTimeout = AmqpCore.Settings.Asynchronous.ReadTimeout;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected override async Task NotifyMessageProcessingStartedAsync(MessageListener listener, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedStartingAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifyAsynchronousMessageReceivedStartingAsync(listener, message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedStartingAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override async Task NotifyMessageProcessingReadyAsync(IAmqpMessage rawMessage, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifyAsynchronousMessageReceivedAsync(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected override async Task NotifyMessageProcessingCompletedAsync(IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedCompletedAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifyAsynchronousMessageReceivedCompletedAsync(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedCompletedAsync), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
    }
}
