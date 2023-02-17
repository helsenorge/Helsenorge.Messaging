﻿/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.Bus.Receivers
{
    /// <summary>
    /// Handles received messages on the error queue
    /// </summary>
    public class ErrorMessageListener : MessageListener
    {
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected override QueueType QueueType => QueueType.Error;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorMessageListener"/> class.
        /// </summary>
        /// <param name="core">An instance of <see cref="ServiceBusCore"/> which has the common infrastructure to talk to the Message Bus.</param>
        /// <param name="logger">An instance of <see cref="ILogger"/>, used to log diagnostics information.</param>
        /// <param name="messagingNotification">An instance of <see cref="IMessagingNotification"/> which holds reference to callbacks back to the client that owns this instance of the <see cref="MessageListener"/>.</param>
        /// <param name="queueNames">The Queue Names associated with the client.</param>
        internal ErrorMessageListener(
            ServiceBusCore core,
            ILogger logger,
            IMessagingNotification messagingNotification,
            QueueNames queueNames) : base(core, logger, messagingNotification, queueNames)
        {
            ReadTimeout = Core.Settings.Error.ReadTimeout;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="listener">Reference to the listener which invoked the callback.</param>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected override async Task NotifyMessageProcessingStarted(MessageListener listener, IncomingMessage message)
        {
            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifyErrorMessageReceivedStarting), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifyErrorMessageReceivedStarting(message).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifyErrorMessageReceivedStarting), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override async Task NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message)
        {
            if (rawMessage == null) throw new ArgumentNullException(nameof(rawMessage));
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"Label: {message.MessageFunction} ");

            // we have received a soap fault
            if (message.MessageFunction.Equals(ServiceBusCore.SoapFaultLabel, StringComparison.OrdinalIgnoreCase) &&
                (message.Payload != null))
            {
                XNamespace soapNs = "http://www.w3.org/2003/05/soap-envelope";
                XNamespace dialogNs = "http://www.kith.no/xmlstds/digitaldialog/2013-10-08";

                if (message.Payload.Root != null)
                {
                    var bodyNode = message.Payload.Root.Element(soapNs + "Body");
                    var faultNode = bodyNode?.Element(soapNs + "Fault");
                    if (faultNode != null)
                    {
                        var valueNode = faultNode.Descendants(soapNs + "Value").FirstOrDefault();
                        if (valueNode != null)
                        {
                            stringBuilder.Append($"FaultCode: {valueNode.Value} ");
                        }
                        var reasonNode = faultNode.Descendants(soapNs + "Text").FirstOrDefault();
                        if (reasonNode != null)
                        {
                            stringBuilder.Append($"FaultReason: \"{reasonNode.Value}\" ");
                        }
                        var messageIdNode = faultNode.Descendants(dialogNs + "messageId").FirstOrDefault();
                        if (messageIdNode != null)
                        {
                            stringBuilder.Append($"MessageId: {messageIdNode.Value} ");
                        }
                        var timestampNode = faultNode.Descendants(dialogNs + "applicationTimeStamp").FirstOrDefault();
                        if (timestampNode != null)
                        {
                            stringBuilder.Append($"ApplicationTimeStamp: {timestampNode.Value} ");
                        }
                    }
                }
            }
            else // we received a message where error codes are stored in properties
            {
                foreach (var property in rawMessage.Properties)
                {
                    stringBuilder.Append($"{property.Key}: {property.Value} ");
                }
            }

            Logger.LogExternalReportedError(stringBuilder.ToString());

            Logger.LogBeforeNotificationHandler(nameof(MessagingNotification.NotifyErrorMessageReceived), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
            await MessagingNotification.NotifyErrorMessageReceived(rawMessage).ConfigureAwait(false);
            Logger.LogAfterNotificationHandler(nameof(MessagingNotification.NotifyErrorMessageReceived), message.MessageFunction, message.FromHerId, message.ToHerId, message.MessageId);
        }

        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected override Task NotifyMessageProcessingCompleted(IncomingMessage message)
        {
            // not relevant for error messages
            return Task.CompletedTask;
        }
    }
}
