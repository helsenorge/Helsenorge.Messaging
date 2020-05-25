using Amqp;
using System.Text;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    internal static class MessageExtensions
    {
        internal static string GetBodyAsString(this Message message)
        {
            return Encoding.UTF8.GetString(message.GetBody<byte[]>());
        }
    }
}
