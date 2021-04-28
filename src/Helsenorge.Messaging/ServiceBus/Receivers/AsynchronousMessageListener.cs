/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    /// <summary>
    /// Handles received messages from the asynchronous queue
    /// </summary>
    public class AsynchronousMessageListener : MessageListener
    {
        /// <summary>
        /// Specifies the name of the queue this listener is reading from
        /// </summary>
        protected override string QueueName => ResolveQueueName((x) => x.AsynchronousQueueName);
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected override QueueType QueueType => QueueType.Asynchronous;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="core">Reference to core service bus infrastructure</param>
        /// <param name="logger">Logger used for diagnostic purposes</param>
        /// <param name="messagingNotification">A reference to the messaging notification system</param>
        internal AsynchronousMessageListener(
            ServiceBusCore core,
            ILogger logger,
            IMessagingNotification messagingNotification) : base(core, logger, messagingNotification)
        {
            ReadTimeout = Core.Settings.Asynchronous.ReadTimeout;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected override async Task NotifyMessageProcessingStarted(MessageListener listener, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedStarting), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifyAsynchronousMessageReceivedStarting(listener, message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedStarting), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override async Task NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceived), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifyAsynchronousMessageReceived(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceived), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected override async Task NotifyMessageProcessingCompleted(IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedCompleted), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifyAsynchronousMessageReceivedCompleted(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifyAsynchronousMessageReceivedCompleted), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
    }
}
