using System;

namespace Helsenorge.Messaging.IntegrationTests.ServiceBus
{
    internal static class ServiceBusTestingConstants
    {
        public static readonly TimeSpan DefaultReadTimeout = TimeSpan.FromSeconds(1);

        public static string GetDeadLetterQueueName(string queueName)
        {
            return $"{queueName}/$deadletterqueue";
        }
    }
}
