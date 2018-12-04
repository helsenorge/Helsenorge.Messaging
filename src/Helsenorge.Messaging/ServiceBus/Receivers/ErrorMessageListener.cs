using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus.Receivers
{
    /// <summary>
    /// Handles received messages on the error queue
    /// </summary>
    public class ErrorMessageListener : MessageListener
    {
        /// <summary>
        /// Specifies the name of the queue this listener is reading from
        /// </summary>
        protected override string QueueName => ResolveQueueName((x) => x.ErrorQueueName);
        /// <summary>
        /// Specifies what type of queue this listener is processing
        /// </summary>
        protected override QueueType QueueType => QueueType.Error;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="core">Reference to core service bus infrastructure</param>
        /// <param name="logger">Logger used for diagnostic purposes</param>
        /// <param name="messagingNotification">A reference to the messaging notification system</param>
        internal ErrorMessageListener(
            ServiceBusCore core,
            ILogger logger,
            IMessagingNotification messagingNotification) : base(core, logger, messagingNotification)
        {
            ReadTimeout = Core.Settings.Error.ReadTimeout;
        }

        /// <summary>
        /// Called prior to message processing
        /// </summary>
        /// <param name="message">Reference to the incoming message. Some fields may not have values since they get populated later in the processing pipeline.</param>
        protected override void NotifyMessageProcessingStarted(IncomingMessage message)
        {
            MessagingNotification.NotifyErrorMessageReceivedStarting(message);
        }
        /// <summary>
        /// Called to process message
        /// </summary>
        /// <param name="rawMessage">The message from the queue</param>
        /// <param name="message">The refined message data. All information should now be present</param>
        protected override void NotifyMessageProcessingReady(IMessagingMessage rawMessage, IncomingMessage message)
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
            MessagingNotification.NotifyErrorMessageReceived(rawMessage);
        }

        /// <summary>
        /// Called when message processing is complete
        /// </summary>
        /// <param name="message">Reference to the incoming message</param>
        protected override void NotifyMessageProcessingCompleted(IncomingMessage message)
        {
            // not relevant for error messages
        }
    }
}
