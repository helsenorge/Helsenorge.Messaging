using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helsenorge.Messaging.Abstractions;
using Helsenorge.Messaging.ServiceBus.Senders;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Helsenorge.Messaging
{
    /// <summary>
    /// Default implementation of <see cref="IMessagingClient"/>. This must act as a singleton, otherwise syncronous messaging will not work
    /// </summary>
    public sealed class MessagingClient : MessagingCore, IMessagingClient
    {
        private readonly AsynchronousSender _asynchronousServiceBusSender;
        private readonly SynchronousSender _synchronousServiceBusSender;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="settings">Set of options to use</param>
        /// <param name="collaborationProtocolRegistry">Reference to the collaboration protocol registry</param>
        /// <param name="addressRegistry">Reference to the address registry</param>
        public MessagingClient(
            MessagingSettings settings,
            ICollaborationProtocolRegistry collaborationProtocolRegistry,
            IAddressRegistry addressRegistry) : base(settings, collaborationProtocolRegistry, addressRegistry)
        {
            _asynchronousServiceBusSender = new AsynchronousSender(ServiceBus);
            _synchronousServiceBusSender = new SynchronousSender(ServiceBus);
        }

        /// <summary>
        /// Sends a message and allows the calling code to continue its work
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message">Information about the message being sent</param>
        /// <returns></returns>
        public async Task SendAndContinueAsync(ILogger logger, OutgoingMessage message)
        {
            var collaborationProtocolMessage = await PreCheck(logger, message).ConfigureAwait(false);

            switch (collaborationProtocolMessage.DeliveryProtocol)
            {
                case DeliveryProtocol.Amqp:
                    await _asynchronousServiceBusSender.SendAsync(logger, message).ConfigureAwait(false);
                    return;
                case DeliveryProtocol.Unknown:
                default:
                    throw new MessagingException("Invalid delivery protocol: " + message.MessageFunction)
                    {
                        EventId = EventIds.InvalidMessageFunction
                    };
            }
        }

        /// <summary>
        /// Sends a message and blocks the calling code until we have an answer
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        /// <returns>The received XML</returns>
        public async Task<XDocument> SendAndWaitAsync(ILogger logger, OutgoingMessage message)
        {
            var collaborationProtocolMessage = await PreCheck(logger, message).ConfigureAwait(false);
           
            switch (collaborationProtocolMessage.DeliveryProtocol)
            {
                case DeliveryProtocol.Amqp:
                    return await _synchronousServiceBusSender.SendAsync(logger, message).ConfigureAwait(false);
                case DeliveryProtocol.Unknown:
                default:
                    throw new MessagingException("Invalid delivery protocol: " + message.MessageFunction)
                    {
                        EventId = EventIds.InvalidMessageFunction
                    };
            }
        }

        /// <summary>
        /// Registers a delegate that should be called when we receive a synchronous reply message. This is where the main reply message processing is done.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        public void RegisterSynchronousReplyMessageReceivedCallback(Action<IncomingMessage> action) => _synchronousServiceBusSender.OnSynchronousReplyMessageReceived = action;

        private async Task<CollaborationProtocolMessage> PreCheck(ILogger logger, OutgoingMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (string.IsNullOrEmpty(message.MessageFunction)) throw new ArgumentNullException(nameof(message.MessageFunction));
            if (message.ToHerId <= 0) throw new ArgumentOutOfRangeException(nameof(message.ToHerId));

            var messageFunction = string.IsNullOrEmpty(message.ReceiptForMessageFunction)
                ? message.MessageFunction
                : message.ReceiptForMessageFunction;

            var profile = await FindProfile(logger, message).ConfigureAwait(false);
            var collaborationProtocolMessage = profile?.FindMessageForReceiver(messageFunction);

            if ((profile != null && profile.Name == Registries.CollaborationProtocolRegistry.DummyPartyName)
                || (collaborationProtocolMessage == null && messageFunction.ToUpper().Contains("DIALOG_INNBYGGER_TIMERESERVASJON")))
            {
                collaborationProtocolMessage = new CollaborationProtocolMessage
                {
                    Name = messageFunction,
                    DeliveryProtocol = DeliveryProtocol.Amqp,
                    Parts = new List<CollaborationProtocolMessagePart>
                    {
                        new CollaborationProtocolMessagePart
                        {
                            MaxOccurrence = 1,
                            MinOccurrence = 0,
                            XmlNamespace = "http://www.kith.no/xmlstds/msghead/2006-05-24",
                            XmlSchema = "MsgHead-v1_2.xsd"
                        },
                        new CollaborationProtocolMessagePart
                        {
                            MaxOccurrence = 1,
                            MinOccurrence = 0,
                            XmlNamespace = "http://www.kith.no/xmlstds/dialog/2013-01-23",
                            XmlSchema = "dialogmelding-1.1.xsd"
                        }
                    }
                };
            }
            else if (collaborationProtocolMessage == null)
            {
                throw new MessagingException("Invalid delivery protocol: " + message.MessageFunction)
                {
                    EventId = EventIds.InvalidMessageFunction
                };
            }

            return collaborationProtocolMessage;
        }

        private async Task<CollaborationProtocolProfile> FindProfile(ILogger logger, OutgoingMessage message)
        {
            var profile = 
                await CollaborationProtocolRegistry.FindAgreementForCounterpartyAsync(logger, message.ToHerId).ConfigureAwait(false) ??
                await CollaborationProtocolRegistry.FindProtocolForCounterpartyAsync(logger, message.ToHerId).ConfigureAwait(false);
            return profile;
        }
    }
}
