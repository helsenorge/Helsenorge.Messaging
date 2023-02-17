﻿/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
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
    /// Listens for the reply to an outgoing synchronous message
    /// </summary>
    public class SynchronousReplyListener : MessageListener
    {
        /// <summary>
        /// Specifies the name of the queue this listener is reading from
        /// </summary>
        protected override string GetQueueName() => Core.Settings.Synchronous.FindReplyQueueForMe();
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected override QueueType QueueType => QueueType.Synchronous;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronousReplyListener"/> class.
        /// </summary>
        /// <param name="core">An instance of <see cref="ServiceBusCore"/> which has the common infrastructure to talk to the Message Bus.</param>
        /// <param name="logger">An instance of <see cref="ILogger"/>, used to log diagnostics information.</param>
        /// <param name="messagingNotification">An instance of <see cref="IMessagingNotification"/> which holds reference to callbacks back to the client that owns this instance of the <see cref="MessageListener"/>.</param>
        internal SynchronousReplyListener(ServiceBusCore core, ILogger logger, IMessagingNotification messagingNotification) : base(core, logger, messagingNotification)
        {
            ReadTimeout = Core.Settings.Synchronous.ReadTimeout;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected override Task NotifyMessageProcessingStarted(MessageListener listener, IncomingMessage message)
        {
            Logger.LogDebug("NotifyMessageProcessingStarted");
            // Not relevant for this implementation
            return Task.CompletedTask;
        }
        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected override Task NotifyMessageProcessingCompleted(IncomingMessage message)
        {
            Logger.LogDebug("NotifyMessageProcessingCompleted");
            // Not relevant for this implementation
            return Task.CompletedTask;
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override async Task NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceived), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifySynchronousMessageReceived(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifySynchronousMessageReceived), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
    }
}
