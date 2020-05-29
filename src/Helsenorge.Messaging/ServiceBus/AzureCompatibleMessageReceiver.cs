using Amqp;
using System;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.ServiceBus
{
    internal class AzureCompatibleMessageReceiver
    {
        private readonly ReceiverLink _receiver;

        public AzureCompatibleMessageReceiver(Session session, string id)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _receiver = new ReceiverLink(session, $"receiver-link-{Guid.NewGuid()}", id);
        }

        public async Task CloseAsync()
        {
            await _receiver.CloseAsync();
        }
    }
}
