using Amqp;
using Amqp.Framing;
using System;
using System.Threading.Tasks;
using Amqp.Types;

namespace Helsenorge.Messaging.ServiceBus
{
    /// <summary>
    /// Analogue of <code>Microsoft.Azure.Amqp.RequestResponseAmqpLink</code>.
    /// </summary>
    internal class RequestResponseAmqpLink
    {
        private static readonly Symbol EntityTypeSymbol = new Symbol("com.microsoft:entity-type");

        private readonly Session _session;
        private readonly SenderLink _sender;
        private readonly ReceiverLink _receiver;
        private readonly string _renewLockReceiverAddress;

        public RequestResponseAmqpLink(Connection connection, string id)
        {
            _session = new Session(connection ?? throw new ArgumentNullException(nameof(connection)));

            var managementNodeAddress = $"{id}/$management";

            var linkId = Guid.NewGuid().ToString("N");
            _renewLockReceiverAddress = linkId;

            var senderAttach = new Attach
            {
                LinkName = $"duplex:${linkId}:sender",
                Handle = 0,
                Target = new Target { Address = managementNodeAddress },
                Properties = new Fields()
            };

            var receiverAttach = new Attach
            {
                LinkName = $"duplex:${linkId}:receiver",
                Handle = 1,
                Source = new Source { Address = managementNodeAddress },
                Target = new Target { Address = _renewLockReceiverAddress },
                Properties = new Fields()
            };

            senderAttach.Properties[EntityTypeSymbol] =
                receiverAttach.Properties[EntityTypeSymbol] =
                    "entity-mgmt";

            _sender = new SenderLink(_session, "request-response-sender", senderAttach, null);
            _receiver = new ReceiverLink(_session, "request-response-receiver", receiverAttach, null);
        }

        public async Task<Message> SendAsync(Message message)
        {
            if (message.Properties == null)
            {
                message.Properties = new Properties();
            }
            message.Properties.ReplyTo = _renewLockReceiverAddress;
            await _sender.SendAsync(message);
            return await _receiver.ReceiveAsync(TimeSpan.FromSeconds(1));
        }

        public async Task CloseAsync()
        {
            await _session.CloseAsync();
            await _sender.CloseAsync();
            await _receiver.CloseAsync();
        }
    }
}
