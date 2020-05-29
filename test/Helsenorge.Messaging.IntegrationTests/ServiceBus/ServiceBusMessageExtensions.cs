using Helsenorge.Messaging.Abstractions;
using System.IO;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    internal static class ServiceBusMessageExtensions
    {
        public static async Task<string> GetBodyAsStingAsync(this IMessagingMessage message)
        {
            using (var streamReader = new StreamReader(message.GetBody()))
            {
                return await streamReader.ReadToEndAsync();
            }
        }
    }
}
